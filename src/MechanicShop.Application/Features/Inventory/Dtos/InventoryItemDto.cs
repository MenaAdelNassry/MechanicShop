namespace MechanicShop.Application.Features.Inventory.Dtos;

public sealed record InventoryItemDto(
    Guid Id,
    string Name,
    decimal Cost,
    int StockQuantity,
    int ReorderLevel,
    bool IsLowStock,
    IReadOnlyList<InventoryTransactionDto>? Transactions = null);