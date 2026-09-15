namespace MechanicShop.Domain.Workorders;

public sealed class WorkOrderTaskPart
{
    public Guid Id { get; private set; }
    public Guid WorkOrderTaskId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal Cost { get; private set; }
    public int Quantity { get; private set; }

    public WorkOrderTaskPart(Guid inventoryItemId, string name, decimal cost, int quantity)
    {
        Id = Guid.CreateVersion7();
        InventoryItemId = inventoryItemId;
        Name = name.Trim();
        Cost = cost;
        Quantity = quantity;
    }

#pragma warning disable CS8618
    private WorkOrderTaskPart() { }
#pragma warning restore CS8618

}