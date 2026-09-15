using MechanicShop.Domain.Common;

namespace MechanicShop.Domain.Workorders.Events;

public sealed record WorkOrderCancelled(Guid WorkOrderId, List<ReservedPartData> ReservedParts) : DomainEvent;