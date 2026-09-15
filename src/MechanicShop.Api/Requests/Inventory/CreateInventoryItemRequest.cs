namespace MechanicShop.Api.Requests.Inventory;

public class CreateInventoryItemRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public int StockQuantity { get; set; }
    public int ReorderLevel { get; set; }
}