namespace MechanicShop.Application.Features.Customers.Dtos;

public sealed record CustomerDto(
    Guid CustomerId,
    string Name,
    string PhoneNumber,
    string Email,
    IReadOnlyList<VehicleDto> Vehicles
);