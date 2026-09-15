using MassTransit;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.EventHandlers;
using MechanicShop.Domain.Workorders.Events;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Consumers;

public sealed class WorkOrderCancelledConsumer(
    IAppDbContext context,
    ILogger<WorkOrderCancelledConsumer> logger)
    : IConsumer<WorkOrderCancelledIntegrationEvent>
{
    public async Task Consume(ConsumeContext<WorkOrderCancelledIntegrationEvent> contextConsumer)
    {
        var message = contextConsumer.Message;
        var ct = contextConsumer.CancellationToken;

        if (message.ReservedParts.Count == 0)
        {
            return;
        }

        var itemIds = message.ReservedParts
            .Select(p => p.InventoryItemId)
            .Distinct()
            .ToList();

        var inventoryItemsMap = await context.InventoryItems
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        foreach (var part in message.ReservedParts)
        {
            if (inventoryItemsMap.TryGetValue(part.InventoryItemId, out var inventoryItem))
            {
                var releaseResult = inventoryItem.ReleaseStock(
                    quantity: part.Quantity,
                    workOrderId: message.WorkOrderId,
                    workOrderTaskId: part.WorkOrderTaskId);

                if (releaseResult.IsError)
                {
                    logger.LogError(
                        "Failed to release stock for Item '{ItemId}': {Error}",
                        inventoryItem.Id, releaseResult.TopError.Description);
                }
            }
        }

        await context.SaveChangesAsync(ct);
    }
}