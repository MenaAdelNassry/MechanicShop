namespace MechanicShop.Application.Features.Identity.Dtos;

public sealed record AppUserDto(
    string UserId,
    string Email,
    IList<string> Roles,
    Guid? EmployeeId = null,
    string? Name = null);