using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.RepairTasks;

public sealed class RepairTaskPart
{
    public Guid InventoryItemId { get; private set; }
    public int Quantity { get; private set; }
    public Guid RepairTaskId { get; private set; }

    private RepairTaskPart() { }

    private RepairTaskPart(Guid repairTaskId, Guid inventoryItemId, int quantity)
    {
        RepairTaskId = repairTaskId;
        InventoryItemId = inventoryItemId;
        Quantity = quantity;
    }

    public static Result<RepairTaskPart> Create(Guid repairTaskId, Guid inventoryItemId, int quantity)
    {
        if (repairTaskId == Guid.Empty)
            return RepairTaskErrors.PartRepairTaskIdRequired;

        if (inventoryItemId == Guid.Empty)
            return RepairTaskErrors.PartInventoryItemIdRequired;

        if (quantity <= 0)
            return RepairTaskErrors.PartQuantityInvalid;

        return new RepairTaskPart(repairTaskId, inventoryItemId, quantity);
    }

    public Result<Updated> UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            return RepairTaskErrors.PartQuantityInvalid;

        Quantity = newQuantity;
        return Result.Updated;
    }
}