using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Common.Settings;
using MechanicShop.Application.Features.Reports.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MechanicShop.Application.Features.Reports.Queries.GetTechnicianProductivity;

public sealed class GetTechnicianProductivityQueryHandler(IAppDbContext context, IOptions<AppSettings> appSettingsOptions)
    : IRequestHandler<GetTechnicianProductivityQuery, Result<TechniciansReportDto>>
{
    private readonly AppSettings _appSettings = appSettingsOptions.Value;

    public async Task<Result<TechniciansReportDto>> Handle(GetTechnicianProductivityQuery query, CancellationToken ct)
    {
        // 1. Convert time range from local to UTC
        var shopOpenTime = _appSettings.OpeningTime;
        var shopCloseTime = _appSettings.ClosingTime;

        var localStart = query.FromDate.ToDateTime(TimeOnly.MinValue);
        var localEnd = query.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, query.TimeZone);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, query.TimeZone);

        // 2. Fetch completed work orders
        var workOrders = await context.WorkOrders
            .Where(w =>
                w.State == WorkOrderState.Completed &&
                (w.ActualCompletedAtUtc >= utcStart && w.ActualCompletedAtUtc < utcEnd) &&
                (!query.LaborId.HasValue || w.LaborId == query.LaborId.Value))
            .Include(w => w.Labor)
            .Include(w => w.RepairTasks)
            .AsNoTracking()
            .ToListAsync(ct);

        if (workOrders.Count == 0)
        {
            return new TechniciansReportDto
            {
                FromDate = query.FromDate,
                ToDate = query.ToDate,
                TotalCompletedOrders = 0,
                AverageEfficiencyPercentage = 0.0,
                TotalLaborRevenue = 0m,
                Technicians = []
            };
        }

        // 3. Group by LaborId
        var groupedByLabor = workOrders
            .GroupBy(w => w.LaborId)
            .ToList();

        var technicianSummaries = new List<TechnicianProductivitySummaryDto>(groupedByLabor.Count);

        foreach (var group in groupedByLabor)
        {
            var laborOrders = group.ToList();
            var firstOrder = laborOrders.First();
            var laborName = firstOrder.Labor != null
                ? $"{firstOrder.Labor.Name.FullName}"
                : "Unknown Technician";

            // Total estimated hours for tasks
            var totalEstimatedMinutes = laborOrders
                .SelectMany(w => w.RepairTasks)
                .Sum(t => (int)t.EstimatedDurationInMins);
            var totalEstimatedHours = Math.Round(totalEstimatedMinutes / 60.0, 2);

            // Total actual hours spent (Excluding overnight non-business hours)
            var totalActualMinutes = laborOrders
                .Sum(w =>
                {
                    var start = w.ActualStartedAtUtc ?? w.StartAtUtc;
                    var end = w.ActualCompletedAtUtc ?? w.EndAtUtc;

                    var localStartTime = TimeZoneInfo.ConvertTime(start, query.TimeZone);
                    var localEndTime = TimeZoneInfo.ConvertTime(end, query.TimeZone);

                    return CalculateWorkingMinutes(localStartTime, localEndTime, shopOpenTime, shopCloseTime);
                });

            var totalActualHours = Math.Round(totalActualMinutes / 60.0, 2);

            // Efficiency percentage = (estimated / actual) * 100
            var efficiency = totalActualMinutes > 0
                ? Math.Round((totalEstimatedMinutes / totalActualMinutes) * 100, 2)
                : 0.0;

            var totalLaborRevenue = laborOrders.Sum(w => w.TotalLaborCost);
            var completedTasksCount = laborOrders.SelectMany(w => w.RepairTasks).Count();

            technicianSummaries.Add(new TechnicianProductivitySummaryDto
            {
                LaborId = group.Key,
                LaborName = laborName,
                CompletedOrdersCount = laborOrders.Count,
                CompletedTasksCount = completedTasksCount,
                TotalEstimatedHours = totalEstimatedHours,
                TotalActualHours = totalActualHours,
                EfficiencyPercentage = efficiency,
                TotalLaborRevenueGenerated = totalLaborRevenue
            });
        }

        // 4. Overall team performance
        var totalCompletedOrders = workOrders.Count;
        var totalTeamLaborRevenue = technicianSummaries.Sum(t => t.TotalLaborRevenueGenerated);
        var avgEfficiency = technicianSummaries.Count > 0
            ? Math.Round(technicianSummaries.Average(t => t.EfficiencyPercentage), 2)
            : 0.0;

        return new TechniciansReportDto
        {
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            TotalCompletedOrders = totalCompletedOrders,
            AverageEfficiencyPercentage = avgEfficiency,
            TotalLaborRevenue = totalTeamLaborRevenue,
            Technicians = technicianSummaries.OrderByDescending(t => t.EfficiencyPercentage).ToList()
        };
    }

    private static double CalculateWorkingMinutes(
        DateTimeOffset start,
        DateTimeOffset end,
        TimeOnly openTime,
        TimeOnly closeTime)
    {
        if (end <= start) return 0.0;

        var totalMinutes = 0.0;
        var startDate = DateOnly.FromDateTime(start.DateTime);
        var endDate = DateOnly.FromDateTime(end.DateTime);

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var dayOpen = new DateTimeOffset(date.ToDateTime(openTime), start.Offset);
            var dayClose = new DateTimeOffset(date.ToDateTime(closeTime), start.Offset);

            var windowStart = start > dayOpen ? start : dayOpen;
            var windowEnd = end < dayClose ? end : dayClose;

            if (windowEnd > windowStart)
            {
                totalMinutes += (windowEnd - windowStart).TotalMinutes;
            }
        }

        return totalMinutes;
    }
}