using MassTransit;

using MechanicShop.Domain.Workorders.Events;

using MediatR;

namespace MechanicShop.Application.Features.WorkOrders.EventHandlers;

public sealed class WorkOrderCompletedDomainEventHandler(
    IPublishEndpoint publishEndpoint) : INotificationHandler<WorkOrderCompleted>
{
    public async Task Handle(WorkOrderCompleted notification, CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(
            new WorkOrderCompletedIntegrationEvent(notification.WorkOrderId),
            cancellationToken);
    }
}

public sealed record WorkOrderCompletedIntegrationEvent(Guid WorkOrderId);
