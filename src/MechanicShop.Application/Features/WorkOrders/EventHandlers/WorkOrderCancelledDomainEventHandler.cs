using MassTransit;
using MechanicShop.Domain.Workorders.Events;
using MediatR;

namespace MechanicShop.Application.Features.WorkOrders.EventHandlers;

public sealed class WorkOrderCancelledDomainEventHandler(
    IPublishEndpoint publishEndpoint) : INotificationHandler<WorkOrderCancelled>
{
    public async Task Handle(WorkOrderCancelled notification, CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(
            new WorkOrderCancelledIntegrationEvent(notification.WorkOrderId, notification.ReservedParts),
            cancellationToken);
    }
}

public sealed record WorkOrderCancelledIntegrationEvent(
    Guid WorkOrderId,
    List<ReservedPartData> ReservedParts);