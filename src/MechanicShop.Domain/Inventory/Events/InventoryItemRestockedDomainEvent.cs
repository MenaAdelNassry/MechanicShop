using MechanicShop.Domain.Common;

namespace MechanicShop.Domain.Inventory.Events;

public sealed record InventoryItemRestockedDomainEvent(
    Guid InventoryItemId,
    int Quantity,
    int NewStockQuantity) : DomainEvent;
