using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Common.Settings;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MechanicShop.Application.Features.WorkOrders.Policies
{
    public class WorkOrderPolicy(IConfiguration configuration, IAppDbContext context, ILogger<WorkOrderPolicy> logger, IOptions<AppSettings> options) : IWorkOrderPolicy
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IAppDbContext _context = context;
        private readonly ILogger<WorkOrderPolicy> _logger = logger;
        private readonly AppSettings _settings = options.Value;

        public TimeOnly OpeningTime => _settings.OpeningTime;
        public TimeOnly ClosingTime => _settings.ClosingTime;
        public TimeZoneInfo WorkshopTimeZone => TimeZoneInfo.FindSystemTimeZoneById(_settings.TimeZone ?? "Egypt Standard Time");

        public async Task<Result<Success>> CheckSpotAvailabilityAsync(Guid spotId, DateTime startAt, DateTime endAt, Guid? excludeWorkOrderId = null, CancellationToken ct = default)
        {
            var isOccupied = await _context.WorkOrders.AsNoTracking().AnyAsync(
                a =>
                a.SpotId == spotId &&
                a.StartAtUtc < endAt &&
                a.EndAtUtc > startAt &&
                (!excludeWorkOrderId.HasValue || a.Id != excludeWorkOrderId.Value),
                ct);

            return isOccupied
                 ? Error.Conflict("MechanicShop_Spot_Full", "The selected time slot is unavailable for the requested services.")
                 : Result.Success;
        }

        public Result<Success> ValidateMinimumRequirement(DateTimeOffset startAt, DateTimeOffset endAt)
        {
            var minMinutes = GetMinimumAppointmentMinutes();

            if ((endAt - startAt) < TimeSpan.FromMinutes(minMinutes))
            {
                return Error.Conflict(
                    "WorkOrder_TooShort",
                    $"WorkOrder duration must be at least {minMinutes} minutes.");
            }

            return Result.Success;
        }

        public async Task<bool> IsLaborOccupied(Guid laborId, Guid excludedWorkOrderId, DateTime startAt, DateTime endAt)
        {
            return await _context.WorkOrders.AsNoTracking()
                .AnyAsync(a =>
                a.LaborId == laborId &&
                a.Id != excludedWorkOrderId &&
                a.StartAtUtc < endAt &&
                a.EndAtUtc > startAt);
        }

        public bool IsOutsideOperatingHours(DateTimeOffset startAt)
        {
            var open = OpeningTime;
            var close = ClosingTime;

            // في حال العمل على مدار 24 ساعة (24/7)
            if (open == close)
            {
                return false;
            }

            // Convert start time to local workshop time
            var localStart = TimeZoneInfo.ConvertTime(startAt, WorkshopTimeZone);
            var startTimeOnly = TimeOnly.FromTimeSpan(localStart.TimeOfDay);

            bool isWithinHours;
            if (close > open)
            {
                // Normal daytime shift (e.g., 08:00 to 18:00)
                isWithinHours = startTimeOnly >= open && startTimeOnly < close;
            }
            else
            {
                // Night shift crossing midnight (e.g., 20:00 to 04:00)
                isWithinHours = startTimeOnly >= open || startTimeOnly < close;
            }

            if (!isWithinHours)
            {
                _logger.LogWarning(
                    "WorkOrder start time '{StartAt}' is outside workshop operating hours ({Opening} - {Closing}).",
                    localStart, open, close);
                return true;
            }

            return false;
        }

        public DateTimeOffset CalculateActualEndAt(DateTimeOffset startAt, TimeSpan duration)
        {
            var open = OpeningTime;
            var close = ClosingTime;

            // If the workshop is open 24/7, direct collection is possible.
            if (open == close)
            {
                return startAt.Add(duration);
            }

            var remainingMinutes = duration.TotalMinutes;
            var current = TimeZoneInfo.ConvertTime(startAt, WorkshopTimeZone);

            while (remainingMinutes > 0)
            {
                // Merge today's date with closing time
                var dayClose = new DateTimeOffset(current.Date + close.ToTimeSpan(), current.Offset);
                var availableMinutesToday = (dayClose - current).TotalMinutes;

                if (availableMinutesToday <= 0)
                {
                    // Move to the next day's opening time
                    current = new DateTimeOffset(current.Date.AddDays(1) + open.ToTimeSpan(), current.Offset);
                    continue;
                }

                if (remainingMinutes <= availableMinutesToday)
                {
                    current = current.AddMinutes(remainingMinutes);
                    remainingMinutes = 0;
                }
                else
                {
                    remainingMinutes -= availableMinutesToday;

                    // Move to the next day's opening time to continue the remaining minutes
                    current = new DateTimeOffset(current.Date.AddDays(1) + open.ToTimeSpan(), current.Offset);
                }
            }

            return current.ToUniversalTime();
        }

        public async Task<bool> IsVehicleAlreadyScheduled(
            Guid vehicleId,
            DateTimeOffset startAt,
            DateTimeOffset endAt,
            Guid? excludedWorkOrderId = null)
        {
            return await _context.WorkOrders.AsNoTracking().
                AnyAsync(a =>
                a.VehicleId == vehicleId &&
                (excludedWorkOrderId == null || a.Id != excludedWorkOrderId) &&
                a.StartAtUtc < endAt &&
                a.EndAtUtc > startAt);
        }

        private int GetMinimumAppointmentMinutes()
        {
            var val = _configuration["AppSettings:MinimumAppointmentDurationInMinutes"];
            return val is not null && int.TryParse(val, out var mm) ? mm : 0;
        }
    }
}
