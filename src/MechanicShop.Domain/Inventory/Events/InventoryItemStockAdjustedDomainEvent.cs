using MechanicShop.Domain.Common;

namespace MechanicShop.Domain.Inventory.Events;

public sealed record InventoryItemStockAdjustedDomainEvent(
    Guid InventoryItemId,
    int Quantity,
    int NewStockQuantity,
    Guid? PerformedBy,
    string? Reason) : DomainEvent;
