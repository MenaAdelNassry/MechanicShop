namespace MechanicShop.Application.Features.Customers.Dtos.History;
public sealed record CustomerVehicleHistoryDto(
    Guid VehicleId,
    string Make,
    string Model,
    int Year,
    string LicensePlate,
    DateTimeOffset? LastServicedAtUtc
);
