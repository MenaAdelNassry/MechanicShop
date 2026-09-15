namespace MechanicShop.Api.Requests.Inventory;

public class UpdateInventoryItemRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public int ReorderLevel { get; set; }
}