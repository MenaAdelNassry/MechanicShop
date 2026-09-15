using MassTransit;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.EventHandlers;

namespace MechanicShop.Application.Features.WorkOrders.Consumers;

public sealed class WorkOrderTrackingUpdatedEventConsumer(IWorkOrderNotifier notifier)
    : IConsumer<WorkOrderTrackingUpdatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<WorkOrderTrackingUpdatedIntegrationEvent> context)
    {
        var message = context.Message;

        await notifier.NotifyCustomerTrackingUpdatedAsync(
            message.TrackingToken,
            message.NewState,
            message.TimestampUtc,
            context.CancellationToken);
    }
}