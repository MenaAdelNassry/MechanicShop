using MechanicShop.Domain.Common;

namespace MechanicShop.Domain.Inventory.Events;

public sealed record InventoryItemLowStockDomainEvent(
    Guid InventoryItemId,
    int StockQuantity,
    int ReorderLevel) : DomainEvent;