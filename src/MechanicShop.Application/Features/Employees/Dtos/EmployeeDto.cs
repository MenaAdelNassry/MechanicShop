namespace MechanicShop.Application.Features.Employees.Dtos;

public sealed record EmployeeDto
{
    public Guid EmployeeId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}