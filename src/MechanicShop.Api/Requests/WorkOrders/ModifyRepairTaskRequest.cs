namespace MechanicShop.Api.Requests.WorkOrders;

public class ModifyRepairTaskRequest
{
    public Guid[] RepairTaskIds { get; set; } = [];
}