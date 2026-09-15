using MassTransit;

using MechanicShop.Domain.Workorders.Enums;
using MechanicShop.Domain.Workorders.Events;

using MediatR;

namespace MechanicShop.Application.Features.WorkOrders.EventHandlers;

public sealed class WorkOrderCollectionModifiedDomainEventHandler(
    IPublishEndpoint publishEndpoint) : INotificationHandler<WorkOrderCollectionModified>
{
    public async Task Handle(WorkOrderCollectionModified notification, CancellationToken cancellationToken)
    {
        await publishEndpoint.Publish(new WorkOrderCollectionModifiedIntegrationEvent(), cancellationToken);

        if (notification is { TrackingToken: { } token, State: { } state } && token != Guid.Empty)
        {
            await publishEndpoint.Publish(
                new WorkOrderTrackingUpdatedIntegrationEvent(
                token,
                state,
                DateTimeOffset.UtcNow), cancellationToken);
        }
    }
}

public sealed record WorkOrderCollectionModifiedIntegrationEvent();

public sealed record WorkOrderTrackingUpdatedIntegrationEvent(
    Guid TrackingToken,
    WorkOrderState NewState,
    DateTimeOffset TimestampUtc
);