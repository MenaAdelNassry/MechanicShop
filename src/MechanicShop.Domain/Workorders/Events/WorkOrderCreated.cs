using MechanicShop.Domain.Common;

namespace MechanicShop.Domain.Workorders.Events;

public sealed record WorkOrderCreated(Guid WorkOrderId, Guid TrackingToken) : DomainEvent;