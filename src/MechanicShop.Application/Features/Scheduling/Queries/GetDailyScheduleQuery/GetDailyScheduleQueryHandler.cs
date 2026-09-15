using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Employees.Mappers;
using MechanicShop.Application.Features.RepairTasks.Mappers;
using MechanicShop.Application.Features.Scheduling.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Customers.Vehicles;
using MechanicShop.Domain.Workorders.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Scheduling.Queries.GetDailyScheduleQuery;

public sealed class GetDailyScheduleQueryHandler(
    IAppDbContext context,
    TimeProvider datetime)
    : IRequestHandler<GetDailyScheduleQuery, Result<ScheduleDto>>
{
    public const int MinutesInDay = 24 * 60;

    public async Task<Result<ScheduleDto>> Handle(GetDailyScheduleQuery query, CancellationToken ct)
    {
        // Resolve TimeZone safely
        var timeZone = GetTimeZone(query.TimeZone);

        // Resolve Today Date based on the actual local timezone (Not raw UTC!)
        var localNow = TimeZoneInfo.ConvertTime(datetime.GetUtcNow(), timeZone);
        var scheduleDate = query.ScheduleDate ?? DateOnly.FromDateTime(localNow.DateTime);

        var localStart = scheduleDate.ToDateTime(TimeOnly.MinValue);
        var localEnd = localStart.AddDays(1);

        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone);

        var localStartDto = new DateTimeOffset(localStart, timeZone.GetUtcOffset(localStart));

        var workOrders = await context.WorkOrders
            .Where(w =>
                w.StartAtUtc < utcEnd &&
                w.EndAtUtc > utcStart &&
                (query.LaborId == null || w.LaborId == query.LaborId))
            .Include(w => w.RepairTasks)
            .Include(w => w.Vehicle)
            .Include(w => w.Labor)
            .AsNoTracking()
            .ToListAsync(ct);

        var utcNow = datetime.GetUtcNow();

        int duration = query.SlotDurationInMinutes <= 0 ? 15 : query.SlotDurationInMinutes;
        int totalSlotsCount = MinutesInDay / duration;

        var spotDtos = new List<SpotDto>();

        foreach (var spot in Enum.GetValues<Spot>())
        {
            var woBySpot = workOrders
                .Where(w => w.Spot == spot)
                .OrderBy(w => w.StartAtUtc)
                .ToList();

            var occupiedRanges = new List<OccupiedRangeDto>(woBySpot.Count);

            foreach (var wo in woBySpot)
            {
                var woLocalStart = TimeZoneInfo.ConvertTime(wo.StartAtUtc, timeZone);
                var woLocalEnd = TimeZoneInfo.ConvertTime(wo.EndAtUtc, timeZone);

                int startSlotIndex = wo.StartAtUtc <= utcStart
                    ? 0
                    : (int)(woLocalStart - localStartDto).TotalMinutes / duration;

                int endSlotIndex = wo.EndAtUtc >= utcEnd
                    ? totalSlotsCount
                    : (int)(woLocalEnd - localStartDto).TotalMinutes / duration;

                startSlotIndex = Math.Clamp(startSlotIndex, 0, totalSlotsCount);
                endSlotIndex = Math.Clamp(endSlotIndex, 0, totalSlotsCount);

                if (startSlotIndex == endSlotIndex && startSlotIndex < totalSlotsCount)
                {
                    endSlotIndex++;
                }

                occupiedRanges.Add(new OccupiedRangeDto
                {
                    WorkOrderId = wo.Id,
                    Spot = spot,
                    StartSlotIndex = startSlotIndex,
                    EndSlotIndex = endSlotIndex,
                    Vehicle = FormatVehicleInfo(wo.Vehicle!),
                    Labor = wo.Labor?.ToDto(),
                    State = wo.State,
                    WorkOrderLocked = !wo.IsEditable,
                    RepairTasks = [.. wo.RepairTasks.Select(rt => rt.ToDto())]
                });
            }

            spotDtos.Add(new SpotDto
            {
                Spot = spot,
                OccupiedRanges = occupiedRanges
            });
        }

        return new ScheduleDto
        {
            OnDate = scheduleDate,
            EndOfDay = utcEnd < utcNow,
            SlotDurationInMinutes = duration,
            TotalSlotsCount = totalSlotsCount,
            Spots = spotDtos
        };
    }

    private static string? FormatVehicleInfo(Vehicle vehicle) =>
        vehicle != null ? $"{vehicle.Make} | {vehicle.LicensePlate}" : null;

    private static TimeZoneInfo GetTimeZone(string? timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
            return TimeZoneInfo.Utc;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}