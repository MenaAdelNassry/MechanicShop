using MechanicShop.Application.Features.Customers.Dtos;
using MechanicShop.Application.Features.Employees.Dtos;
using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Domain.Workorders.Enums;

namespace MechanicShop.Application.Features.WorkOrders.Dtos;

public sealed record WorkOrderDto
{
    public Guid WorkOrderId { get; init; }
    public Guid? InvoiceId { get; init; }
    public Spot Spot { get; init; }
    public VehicleDto? Vehicle { get; init; }
    public DateTimeOffset StartAtUtc { get; init; }
    public DateTimeOffset EndAtUtc { get; init; }
    public IReadOnlyList<RepairTaskDto> RepairTasks { get; init; } = [];
    public EmployeeDto? Labor { get; init; }
    public WorkOrderState State { get; init; }
    public decimal TotalPartCost { get; init; }
    public decimal TotalLaborCost { get; init; }
    public decimal TotalCost { get; init; }
    public int TotalDurationInMins { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ActualStartedAtUtc { get; init; }
    public DateTimeOffset? ActualCompletedAtUtc { get; init; }
}