namespace MechanicShop.Application.Features.Scheduling.Dtos;

public sealed record ScheduleDto
{
    public DateOnly OnDate { get; init; }
    public bool EndOfDay { get; init; }
    public int SlotDurationInMinutes { get; init; }
    public int TotalSlotsCount { get; init; }
    public IReadOnlyList<SpotDto> Spots { get; init; } = [];
}