using MechanicShop.Domain.Common;

namespace MechanicShop.Domain.Inventory.Events;

public sealed record InventoryItemStockReservedDomainEvent(
    Guid InventoryItemId,
    int Quantity,
    Guid? WorkOrderId,
    Guid? WorkOrderTaskId,
    int NewStockQuantity) : DomainEvent;
