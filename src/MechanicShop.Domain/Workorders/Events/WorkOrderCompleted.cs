using MechanicShop.Domain.Common;

namespace MechanicShop.Domain.Workorders.Events;

public sealed record WorkOrderCompleted(Guid WorkOrderId) : DomainEvent;