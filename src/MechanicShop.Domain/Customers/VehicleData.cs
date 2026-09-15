namespace MechanicShop.Domain.Customers;

public sealed record VehicleData(
    Guid? Id,
    string Make,
    string Model,
    int Year,
    string LicensePlate);