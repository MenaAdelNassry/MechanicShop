using MechanicShop.Domain.RepairTasks.Enums;
using MechanicShop.Domain.Workorders;

namespace MechanicShop.Tests.Common.WorkOrders;

public static class WorkOrderTaskFactory
{
    public static WorkOrderTask CreateTask(
        Guid? id = null,
        Guid? originalTaskId = null,
        string name = "Brake Replacement",
        decimal laborCost = 150m,
        RepairDurationInMinutes estimatedDurationInMins = RepairDurationInMinutes.Min30,
        List<WorkOrderTaskPart>? parts = null)
    {
        return new WorkOrderTask(
            id ?? Guid.CreateVersion7(),
            originalTaskId ?? Guid.CreateVersion7(),
            name,
            laborCost,
            estimatedDurationInMins,
            parts ?? [WorkOrderTaskPartFactory.CreatePart()]);
    }
}