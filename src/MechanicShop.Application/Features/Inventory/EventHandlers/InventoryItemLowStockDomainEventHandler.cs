using MassTransit;
using MechanicShop.Domain.Inventory.Events;
using MediatR;

namespace MechanicShop.Application.Features.Inventory.EventHandlers;

public sealed class InventoryItemLowStockDomainEventHandler(
    IPublishEndpoint publishEndpoint) : INotificationHandler<InventoryItemLowStockDomainEvent>
{
    public async Task Handle(InventoryItemLowStockDomainEvent notification, CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(
            new InventoryItemLowStockIntegrationEvent(
                notification.InventoryItemId,
                notification.StockQuantity,
                notification.ReorderLevel),
            cancellationToken);
    }
}

public sealed record InventoryItemLowStockIntegrationEvent(
    Guid InventoryItemId,
    int StockQuantity,
    int ReorderLevel);