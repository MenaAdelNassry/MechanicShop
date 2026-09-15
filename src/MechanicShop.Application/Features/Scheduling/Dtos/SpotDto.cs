using MechanicShop.Domain.Workorders.Enums;

namespace MechanicShop.Application.Features.Scheduling.Dtos;

public sealed record SpotDto
{
    public Spot Spot { get; init; }
    public IReadOnlyList<OccupiedRangeDto> OccupiedRanges { get; init; } = [];
}