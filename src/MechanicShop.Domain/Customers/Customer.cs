using MechanicShop.Domain.Common;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Common.ValueObjects;
using MechanicShop.Domain.Customers.Vehicles;

namespace MechanicShop.Domain.Customers;

public sealed class Customer : AuditableEntity
{
    public PersonName Name { get; private set; }
    public EmailAddress Email { get; private set; }
    public PhoneNumber PhoneNumber { get; private set; }

    private readonly List<Vehicle> _vehicles = [];
    public IEnumerable<Vehicle> Vehicles => _vehicles.AsReadOnly();

    #pragma warning disable CS8618
    private Customer()
    { }

    private Customer(Guid id, PersonName name, EmailAddress email, PhoneNumber phoneNumber, List<Vehicle> vehicles)
        : base(id)
    {
        Name = name;
        PhoneNumber = phoneNumber;
        Email = email;
        _vehicles = vehicles;
    }

    public static Result<Customer> Create(Guid id, PersonName name, EmailAddress email, PhoneNumber phoneNumber, List<Vehicle> vehicles)
    {
        if (id == Guid.Empty) return CustomerErrors.IdRequired;

        return new Customer(id, name, email, phoneNumber, vehicles);
    }

    public Result<Updated> Update(PersonName name, EmailAddress email, PhoneNumber phoneNumber)
    {
        Name = name;
        Email = email;
        PhoneNumber = phoneNumber;

        return Result.Updated;
    }

    public Result<Updated> UpsertVehicles(IReadOnlyList<VehicleData> incomingVehicles, TimeProvider timeProvider)
    {
        var incomingIds = incomingVehicles
            .Where(v => v.Id.HasValue && v.Id.Value != Guid.Empty)
            .Select(v => v.Id!.Value)
            .ToHashSet();

        // 1. Remove any vehicles that are not in the incoming list
        _vehicles.RemoveAll(existing => !incomingIds.Contains(existing.Id));

        // 2. Update existing vehicles or create new ones
        foreach (var incoming in incomingVehicles)
        {
            if (incoming.Id.HasValue && incoming.Id.Value != Guid.Empty)
            {
                var existingVehicle = _vehicles.FirstOrDefault(v => v.Id == incoming.Id.Value);
                if (existingVehicle is not null)
                {
                    var updateResult = existingVehicle.Update(
                        incoming.Make,
                        incoming.Model,
                        incoming.Year,
                        incoming.LicensePlate,
                        timeProvider);

                    if (updateResult.IsError) return updateResult.Errors;
                }
            }
            else
            {
                var createResult = Vehicle.Create(
                    Guid.CreateVersion7(),
                    incoming.Make,
                    incoming.Model,
                    incoming.Year,
                    incoming.LicensePlate,
                    Id,
                    timeProvider);

                if (createResult.IsError) return createResult.Errors;

                _vehicles.Add(createResult.Value);
            }
        }

        return Result.Updated;
    }
}