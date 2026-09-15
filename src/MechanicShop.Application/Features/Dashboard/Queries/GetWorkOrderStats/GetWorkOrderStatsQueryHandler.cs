using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Dashboard.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Dashboard.Queries.GetWorkOrderStats;

public sealed class GetWorkOrderStatsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetWorkOrderStatsQuery, Result<TodayWorkOrderStatsDto>>
{
    public async Task<Result<TodayWorkOrderStatsDto>> Handle(GetWorkOrderStatsQuery request, CancellationToken ct)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId ?? "Africa/Cairo");

        var localStart = request.Date.ToDateTime(TimeOnly.MinValue);
        var localEnd = request.Date.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone);

        var workOrders = await context.WorkOrders
            .Where(w => w.StartAtUtc >= utcStart && w.StartAtUtc < utcEnd)
            .Include(w => w.Vehicle)
            .Include(w => w.RepairTasks)
                .ThenInclude(rt => rt.Parts)
            .Include(w => w.Invoice)
                .ThenInclude(i => i!.LineItems)
            .AsNoTracking()
            .ToListAsync(ct);

        if (workOrders.Count == 0)
        {
            return new TodayWorkOrderStatsDto { Date = request.Date };
        }

        var total = workOrders.Count;
        var scheduled = workOrders.Count(w => w.State == WorkOrderState.Scheduled);
        var inProgress = workOrders.Count(w => w.State == WorkOrderState.InProgress);
        var completed = workOrders.Count(w => w.State == WorkOrderState.Completed);
        var cancelled = workOrders.Count(w => w.State == WorkOrderState.Cancelled);

        var realizedOrders = workOrders
            .Where(w => w.Invoice != null)
            .ToList();

        var totalRevenue = workOrders
            .Where(w => w.Invoice != null)
            .Sum(w => w.Invoice!.Total);

        var totalPartsCost = realizedOrders.Sum(w => w.TotalPartsCost);
        var totalLaborCost = realizedOrders.Sum(w => w.TotalLaborCost);

        var uniqueVehicles = workOrders.Select(w => w.VehicleId).Distinct().Count();
        var uniqueCustomers = workOrders.Where(w => w.Vehicle != null).Select(w => w.Vehicle!.CustomerId).Distinct().Count();

        var netProfit = totalRevenue - totalPartsCost - totalLaborCost;

        return new TodayWorkOrderStatsDto
        {
            Date = request.Date,
            Total = total,
            Scheduled = scheduled,
            InProgress = inProgress,
            Completed = completed,
            Cancelled = cancelled,
            TotalRevenue = totalRevenue,
            TotalPartsCost = totalPartsCost,
            TotalLaborCost = totalLaborCost,
            UniqueVehicles = uniqueVehicles,
            UniqueCustomers = uniqueCustomers,
            NetProfit = netProfit,
            ProfitMargin = totalRevenue > 0 ? (netProfit / totalRevenue) * 100 : 0,
            CompletionRate = total > 0 ? ((decimal)completed / total) * 100 : 0,
            AverageRevenuePerOrder = completed > 0 ? totalRevenue / completed : 0,
            OrdersPerVehicle = uniqueVehicles > 0 ? (decimal)total / uniqueVehicles : 0,
            PartsCostRatio = totalRevenue > 0 ? (totalPartsCost / totalRevenue) * 100 : 0,
            LaborCostRatio = totalRevenue > 0 ? (totalLaborCost / totalRevenue) * 100 : 0,
            CancellationRate = total > 0 ? ((decimal)cancelled / total) * 100 : 0
        };
    }
}