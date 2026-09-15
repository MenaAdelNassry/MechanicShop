using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Application.Common.Interfaces;

public interface IWorkOrderPolicy
{
    TimeOnly OpeningTime { get; }
    TimeOnly ClosingTime { get; }
    TimeZoneInfo WorkshopTimeZone { get; }

    bool IsOutsideOperatingHours(DateTimeOffset startAt);
    DateTimeOffset CalculateActualEndAt(DateTimeOffset startAt, TimeSpan duration);
    Result<Success> ValidateMinimumRequirement(DateTimeOffset startAt, DateTimeOffset endAt);
}