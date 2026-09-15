using MechanicShop.Domain.Workorders;

namespace MechanicShop.Tests.Common.WorkOrders
{
    public static class WorkOrderTaskPartFactory
    {
        public static WorkOrderTaskPart CreatePart(
            Guid? originalPartId = null,
            string name = "Brake Pads",
            decimal cost = 100m,
            int quantity = 2)
        {
            return new WorkOrderTaskPart(
                originalPartId ?? Guid.CreateVersion7(),
                name,
                cost,
                quantity);
        }
    }
}
