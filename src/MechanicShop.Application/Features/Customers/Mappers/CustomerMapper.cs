using MechanicShop.Application.Features.Customers.Dtos;
using MechanicShop.Domain.Customers;
using MechanicShop.Domain.Customers.Vehicles;

namespace MechanicShop.Application.Features.Customers.Mappers;

public static class CustomerMapper
{
    public static CustomerDto ToDto(this Customer entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new CustomerDto(
            CustomerId: entity.Id,
            Name: $"{entity.Name.FirstName} {entity.Name.LastName}",
            Email: entity.Email.Value,
            PhoneNumber: entity.PhoneNumber.Value,
            Vehicles: entity.Vehicles?.Select(v => v.ToDto()).ToList() ?? []);
    }

    public static List<CustomerDto> ToDtos(this IEnumerable<Customer> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        return [.. entities.Select(e => e.ToDto())];
    }

    public static VehicleDto ToDto(this Vehicle entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return new VehicleDto(entity.Id, entity.Make!, entity.Model!, entity.Year, entity.LicensePlate!);
    }

    public static List<VehicleDto> ToDtos(this IEnumerable<Vehicle> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        return [.. entities.Select(e => e.ToDto())];
    }
}