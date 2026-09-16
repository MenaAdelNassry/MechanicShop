using MechanicShop.Application.Features.Customers.Dtos;
using MechanicShop.Domain.Workorders.Enums;

namespace MechanicShop.Application.Features.WorkOrders.Dtos;

public sealed record WorkOrderListItemDto
{
    public Guid WorkOrderId { get; init; }
    public Guid? InvoiceId { get; init; }
    public VehicleDto? Vehicle { get; init; }
    public string? Customer { get; init; }
    public string? Labor { get; init; }
    public WorkOrderState State { get; init; }
    public Guid SpotId { get; init; }
    public string SpotName { get; init; } = string.Empty;
    public DateTimeOffset StartAtUtc { get; init; }
    public DateTimeOffset EndAtUtc { get; init; }
    public IReadOnlyList<string> RepairTasks { get; init; } = [];
}