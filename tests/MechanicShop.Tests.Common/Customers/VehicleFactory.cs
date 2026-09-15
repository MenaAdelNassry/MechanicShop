using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Customers.Vehicles;

namespace MechanicShop.Tests.Common.Customers;

public static class VehicleFactory
{
    public static Result<Vehicle> CreateVehicle(Guid? id = null, string? make = null, string? model = null, int? year = null, string? licensePlate = null, Guid? customerId = null, TimeProvider? timeProvider = null)
    {
        var effectiveTime = timeProvider ?? new FakeTimeProvider();
        var code = GenerateCode();
        if (timeProvider is null && effectiveTime is FakeTimeProvider fake)
        {
            fake.SetUtcNow(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        }

        return Vehicle.Create(
            id ?? Guid.CreateVersion7(),
            make ?? "Honda",
            model ?? "Accord",
            year ?? 2024,
            licensePlate ?? ShortLicense(code),
            customerId ?? Guid.CreateVersion7(),
            effectiveTime);
    }

    public static string ShortLicense(string prefix, int maxLen = 12)
    {
        var id = Guid.CreateVersion7().ToString("N");
        var combined = $"{prefix}-{id}";
        return combined.Length <= maxLen ? combined : combined.Substring(0, maxLen);
    }

    private static string GenerateCode()
    {
        var random = new Random();
        return string.Concat(Enumerable.Range(0, 3)
            .Select(_ => (char)random.Next('A', 'Z' + 1)));
    }
}