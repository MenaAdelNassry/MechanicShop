using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Inventory;

namespace MechanicShop.Tests.Common.Inventory;

public static class InventoryItemFactory
{
    public static Result<InventoryItem> CreateItem(
        Guid? id = null,
        string? name = null,
        decimal? cost = null,
        int? stockQuantity = null,
        int? reorderLevel = null)
    {
        return InventoryItem.Create(
            id ?? Guid.CreateVersion7(),
            name ?? "Brake Pad",
            cost ?? 50.00m,
            stockQuantity ?? 100,
            reorderLevel ?? 10);
    }
}