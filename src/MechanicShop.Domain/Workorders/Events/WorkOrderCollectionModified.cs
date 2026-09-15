using MechanicShop.Domain.Common;
using MechanicShop.Domain.Workorders.Enums;

namespace MechanicShop.Domain.Workorders.Events;

public sealed record WorkOrderCollectionModified(Guid? TrackingToken = null, WorkOrderState? State = null) : DomainEvent;