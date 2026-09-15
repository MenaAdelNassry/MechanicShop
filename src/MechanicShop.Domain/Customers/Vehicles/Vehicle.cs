using MechanicShop.Domain.Common;
using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.Customers.Vehicles;

public sealed class Vehicle : AuditableEntity
{
    public Guid CustomerId { get; private set; }
    public string Make { get; private set; }
    public string Model { get; private set; }
    public int Year { get; private set; }
    public string LicensePlate { get; private set; }
    public Customer? Customer { get; internal set; }

    public string VehicleInfo => $"{Make} | {Model} | {Year}";

#pragma warning disable CS8618
    private Vehicle() { }
#pragma warning restore CS8618

    private Vehicle(Guid id, string make, string model, int year, string licensePlate, Guid customerId)
        : base(id)
    {
        Make = make;
        Model = model;
        Year = year;
        LicensePlate = licensePlate;
        CustomerId = customerId;
    }

    public static Result<Vehicle> Create(
        Guid id,
        string make,
        string model,
        int year,
        string licensePlate,
        Guid customerId,
        TimeProvider timeProvider)
    {
        var validationResult = Validate(make, model, year, licensePlate, timeProvider);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }

        return new Vehicle(
            id,
            make.Trim(),
            model.Trim(),
            year,
            licensePlate.Trim().ToUpperInvariant(),
            customerId);
    }

    public Result<Updated> Update(
        string make,
        string model,
        int year,
        string licensePlate,
        TimeProvider timeProvider)
    {
        var validationResult = Validate(make, model, year, licensePlate, timeProvider);
        if (validationResult.IsError)
        {
            return validationResult.Errors;
        }

        Make = make.Trim();
        Model = model.Trim();
        Year = year;
        LicensePlate = licensePlate.Trim().ToUpperInvariant();

        return Result.Updated;
    }

    private static Result<Updated> Validate(
        string make,
        string model,
        int year,
        string licensePlate,
        TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(make))
            return VehicleErrors.MakeRequired;

        if (string.IsNullOrWhiteSpace(model))
            return VehicleErrors.ModelRequired;

        if (string.IsNullOrWhiteSpace(licensePlate))
            return VehicleErrors.LicensePlateRequired;

        var maxAllowedYear = timeProvider.GetUtcNow().Year + 1;
        if (year < 1886 || year > maxAllowedYear)
            return VehicleErrors.YearInvalid;

        return Result.Updated;
    }
}