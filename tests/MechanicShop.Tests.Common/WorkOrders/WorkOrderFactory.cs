using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders;
using MechanicShop.Domain.Workorders.Enums;

namespace MechanicShop.Tests.Common.WorkOrders;

public static class WorkOrderFactory
{
    // 💡 Centralized helper method to remove repetitive Create blocks (DRY Principle)
    public static Result<WorkOrder> CreateTestWorkOrder(
        Guid? id = null,
        Guid? vehicleId = null,
        DateTimeOffset? startAt = null,
        DateTimeOffset? endAt = null,
        Guid? laborId = null,
        Spot? spot = null,
        List<WorkOrderTask>? repairTasks = null)
    {
        return WorkOrder.Create(
            id: id ?? Guid.CreateVersion7(),
            vehicleId: vehicleId ?? Guid.CreateVersion7(),
            startAt: startAt ?? DateTimeOffset.UtcNow,
            endAt: endAt ?? DateTimeOffset.UtcNow.AddHours(1),
            laborId: laborId ?? Guid.CreateVersion7(),
            spot: spot ?? Spot.A,
            repairTasks: repairTasks ?? [WorkOrderTaskFactory.CreateTask()]);
    }
}