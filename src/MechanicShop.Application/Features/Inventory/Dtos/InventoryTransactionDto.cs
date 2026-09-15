namespace MechanicShop.Application.Features.Inventory.Dtos;

public sealed record InventoryTransactionDto(
    Guid Id,
    Guid InventoryItemId,
    string Type,
    int Quantity,
    Guid? WorkOrderId,
    Guid? WorkOrderTaskId,
    Guid? PerformedBy,
    string? Reason,
    DateTimeOffset OccurredAtUtc);