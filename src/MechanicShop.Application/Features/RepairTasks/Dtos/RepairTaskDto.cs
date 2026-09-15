using MechanicShop.Domain.RepairTasks.Enums;

namespace MechanicShop.Application.Features.RepairTasks.Dtos;

public sealed record RepairTaskDto
{
    public Guid? Id { get; init; }
    public Guid OriginalRepairTaskId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal LaborCost { get; init; }
    public decimal TotalCost { get; init; }
    public DateTimeOffset? DeletedAtUtc { get; init; }
    public RepairDurationInMinutes EstimatedDurationInMins { get; init; }
    public List<RepairTaskPartDto> Parts { get; init; } = [];
}