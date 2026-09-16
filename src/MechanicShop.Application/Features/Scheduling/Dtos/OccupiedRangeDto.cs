using MechanicShop.Application.Features.Employees.Dtos;
using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Domain.Workorders.Enums;

namespace MechanicShop.Application.Features.Scheduling.Dtos;

public sealed record OccupiedRangeDto
{
    public Guid WorkOrderId { get; init; }
    public string SpotName { get; init; } = string.Empty;
    public int StartSlotIndex { get; init; }
    public int EndSlotIndex { get; init; }
    public string? Vehicle { get; init; }
    public EmployeeDto? Labor { get; init; }
    public WorkOrderState State { get; init; }
    public bool WorkOrderLocked { get; init; }
    public IReadOnlyList<RepairTaskDto> RepairTasks { get; init; } = [];
}