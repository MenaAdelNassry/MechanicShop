namespace MechanicShop.Api.Responses;

public sealed record OperatingHoursResponse(TimeOnly OpeningTime, TimeOnly ClosingTime);