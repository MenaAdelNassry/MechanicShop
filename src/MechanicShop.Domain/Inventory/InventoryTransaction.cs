using MechanicShop.Domain.Common;

namespace MechanicShop.Domain.Inventory;

public enum InventoryTransactionType
{
    Reserved = 1,
    Released = 2,
    Restocked = 3,
    Adjustment = 4
}

public sealed class InventoryTransaction : Entity
{
    public Guid InventoryItemId { get; private set; }
    public InventoryTransactionType Type { get; private set; }
    public int Quantity { get; private set; }
    public Guid? WorkOrderId { get; private set; }
    public Guid? WorkOrderTaskId { get; private set; }
    public Guid? PerformedBy { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    internal InventoryTransaction(
        Guid inventoryItemId,
        InventoryTransactionType type,
        int quantity,
        Guid? workOrderId = null,
        Guid? workOrderTaskId = null,
        Guid? performedBy = null,
        string? reason = null,
        DateTimeOffset? occurredAtUtc = null)
        : base(Guid.CreateVersion7())
    {
        InventoryItemId = inventoryItemId;
        Type = type;
        Quantity = quantity;
        WorkOrderId = workOrderId;
        WorkOrderTaskId = workOrderTaskId;
        PerformedBy = performedBy;
        Reason = reason;
        OccurredAtUtc = occurredAtUtc ?? DateTimeOffset.UtcNow;
    }

#pragma warning disable CS8618
    private InventoryTransaction() { }
#pragma warning restore CS8618
}