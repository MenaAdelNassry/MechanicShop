using MechanicShop.Domain.Workorders.Enums;

namespace MechanicShop.Api.Requests.WorkOrders;

public class UpdateWorkOrderStateRequest
{
    public WorkOrderState State { get; set; }
}