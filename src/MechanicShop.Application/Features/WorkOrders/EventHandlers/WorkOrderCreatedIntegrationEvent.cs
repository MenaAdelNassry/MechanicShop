using MassTransit;

using MechanicShop.Domain.Workorders.Events;

using MediatR;

namespace MechanicShop.Application.Features.WorkOrders.EventHandlers;

public sealed record WorkOrderCreatedIntegrationEvent(Guid WorkOrderId, Guid TrackingToken);

public sealed class WorkOrderCreatedDomainEventHandler(
    IPublishEndpoint publishEndpoint) : INotificationHandler<WorkOrderCreated>
{
    public async Task Handle(WorkOrderCreated notification, CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(
            new WorkOrderCreatedIntegrationEvent(notification.WorkOrderId, notification.TrackingToken),
            cancellationToken);
    }
}