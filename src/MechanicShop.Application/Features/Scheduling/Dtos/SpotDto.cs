namespace MechanicShop.Application.Features.Scheduling.Dtos;

public sealed record SpotDto(
    Guid SpotId,
    string SpotName,
    IReadOnlyList<OccupiedRangeDto> OccupiedRanges);