using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.RepairTasks;

namespace MechanicShop.Tests.Common.RepairTasks;

public static class RepairTaskPartFactory
{
    public static Result<RepairTaskPart> CreatePart(Guid? inventoryItemId = null, int? quantity = null)
    {
        return RepairTaskPart.Create(
            inventoryItemId ?? Guid.CreateVersion7(),
            quantity ?? 2);
    }
}