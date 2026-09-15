using System.ComponentModel.DataAnnotations;

namespace MechanicShop.Api.Requests.RepairTasks;

public class UpdateRepairTaskPartRequest
{
    [Required(ErrorMessage = "InventoryItem ID is required.")]
    public Guid InventoryItemId { get; set; }

    [Required(ErrorMessage = "Quantity is required.")]
    [Range(1, 100, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; }
}