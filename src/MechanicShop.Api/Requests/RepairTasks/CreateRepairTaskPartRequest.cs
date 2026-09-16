using System.ComponentModel.DataAnnotations;

namespace MechanicShop.Api.Requests.RepairTasks;

public class CreateRepairTaskPartRequest
{
    public Guid InventoryItemId { get; set; }

    public int Quantity { get; set; }
}