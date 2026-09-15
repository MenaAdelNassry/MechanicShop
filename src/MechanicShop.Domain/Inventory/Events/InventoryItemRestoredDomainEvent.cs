using MechanicShop.Domain.Common;

namespace MechanicShop.Domain.Inventory.Events;

public sealed record InventoryItemRestoredDomainEvent(Guid InventoryItemId) : DomainEvent;
