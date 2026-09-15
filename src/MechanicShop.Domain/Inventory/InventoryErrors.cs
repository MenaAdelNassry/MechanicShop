using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.Inventory;

public static class InventoryErrors
{
    public static readonly Error NameRequired =
        Error.Validation("Inventory.Name.Required", "Inventory item name is required.");

    public static readonly Error CostInvalid =
        Error.Validation("Inventory.Cost.Invalid", "Inventory cost must be greater than 0.");

    public static readonly Error StockInvalid =
        Error.Validation("Inventory.Stock.Invalid", "Stock quantity cannot be negative.");

    public static readonly Error ReorderLevelInvalid =
        Error.Validation("Inventory.ReorderLevel.Invalid", "Reorder level cannot be negative.");

    public static readonly Error QuantityInvalid =
        Error.Validation("Inventory.Quantity.Invalid", "Quantity must be greater than zero.");

    public static Error InsufficientStock(int requested, int available)
        => Error.Conflict("Inventory.Stock.Insufficient", $"Insufficient stock. Requested: {requested}, Available: {available}");

    public static readonly Error AdjustmentReasonRequired =
        Error.Validation("Inventory.Adjustment.ReasonRequired", "Adjustment reason is required.");

    public static readonly Error AlreadyDeleted =
        Error.Conflict("Inventory.AlreadyDeleted", "Inventory item is already deleted.");

    public static readonly Error InvalidAdjustment =
        Error.Validation("Inventory.Adjustment.Invalid", "Adjustment cannot result in negative stock, and quantity cannot be zero.");
}
