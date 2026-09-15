using MassTransit;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.EventHandlers;

namespace MechanicShop.Application.Features.WorkOrders.Consumers;

public sealed class WorkOrderCollectionModifiedEventConsumer(IWorkOrderNotifier notifier)
    : IConsumer<WorkOrderCollectionModifiedIntegrationEvent>
{
    public Task Consume(ConsumeContext<WorkOrderCollectionModifiedIntegrationEvent> context)
    {
        return notifier.NotifyWorkOrdersChangedAsync(context.CancellationToken);
    }
}