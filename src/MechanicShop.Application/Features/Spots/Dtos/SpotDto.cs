namespace MechanicShop.Application.Features.Spots.Dtos;

public sealed record SpotDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive);