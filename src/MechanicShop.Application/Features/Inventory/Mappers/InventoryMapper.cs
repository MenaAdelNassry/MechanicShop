using MechanicShop.Application.Features.Inventory.Dtos;
using MechanicShop.Domain.Inventory;

namespace MechanicShop.Application.Features.Inventory.Mappers;

public static class InventoryMapper
{
    public static InventoryItemDto ToDto(this InventoryItem item)
    {
        return new InventoryItemDto(
            item.Id,
            item.Name,
            item.Cost,
            item.StockQuantity,
            item.ReorderLevel,
            item.StockQuantity <= item.ReorderLevel,
            item.Transactions?.ToDtos());
    }

    public static InventoryTransactionDto ToDto(this InventoryTransaction transaction)
    {
        return new InventoryTransactionDto(
            transaction.Id,
            transaction.InventoryItemId,
            transaction.Type.ToString(),
            transaction.Quantity,
            transaction.WorkOrderId,
            transaction.WorkOrderTaskId,
            transaction.PerformedBy,
            transaction.Reason,
            transaction.OccurredAtUtc);
    }

    public static IReadOnlyList<InventoryTransactionDto> ToDtos(this IEnumerable<InventoryTransaction>? transactions)
    {
        if (transactions is null) return [];
        return transactions.Select(t => t.ToDto()).ToList();
    }

    public static LowStockItemDto ToLowStockDto(this InventoryItem item) =>
        new(item.Id, item.Name, item.StockQuantity, item.ReorderLevel);
}