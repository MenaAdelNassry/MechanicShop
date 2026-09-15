namespace MechanicShop.Application.Features.Inventory.Notifications;

public static class LowStockEmailBuilder
{
    public const string Subject = "⚠️ Low Stock Alert: Restock Required";
    public const string Title = "Inventory Restock Required";

    public static string BuildHtml(string itemName, int currentStock, int reorderLevel) => $@"
        Attention: Inventory item <strong>'{itemName}'</strong> has reached or dropped below its safety threshold.
        <div style='margin-top: 14px; background: #ffffff; padding: 14px; border-radius: 8px; border: 1px solid #e2e8f0;'>
            <strong>Item Name:</strong> {itemName}<br/>
            <strong>Current Stock:</strong> <span style='color: #ef4444; font-weight: bold;'>{currentStock}</span><br/>
            <strong>Reorder Level:</strong> {reorderLevel}
        </div>
        <p style='margin-top: 12px;'>Please place a purchase order for this item as soon as possible.</p>";

    public static string BuildPlainText(string itemName, int currentStock, int reorderLevel) =>
        $"Attention: Inventory item '{itemName}' has reached its reorder level.\n\n" +
        $"Current Stock: {currentStock}\n" +
        $"Reorder Level: {reorderLevel}\n\n" +
        $"Please restock this item as soon as possible.";
}