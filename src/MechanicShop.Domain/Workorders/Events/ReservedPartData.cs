namespace MechanicShop.Domain.Workorders.Events;

public sealed record ReservedPartData(
    Guid InventoryItemId,
    int Quantity,
    Guid WorkOrderTaskId
);