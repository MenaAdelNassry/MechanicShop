using MechanicShop.Application.Features.WorkOrders.Commands.CreateWorkOrder;
using MechanicShop.Domain.Workorders.Enums;

namespace MechanicShop.Tests.Common.WorkOrders;

public static class WorkOrderCommandFactory
{
    public static CreateWorkOrderCommand CreateCreateWorkOrderCommand(
        Spot? spot = null,
        Guid? vehicleId = null,
        DateTimeOffset? startAt = null,
        List<Guid>? repairTaskIds = null,
        Guid? laborId = null)
    {
        return new CreateWorkOrderCommand(
            spot ?? Spot.A,
            vehicleId ?? Guid.CreateVersion7(),
            startAt ?? DateTimeOffset.UtcNow.AddDays(1).Date.AddHours(9),
            repairTaskIds ?? [Guid.CreateVersion7()],
            laborId ?? Guid.CreateVersion7());
    }
}