namespace MechanicShop.Application.Features.Inventory.Dtos;

public sealed record LowStockItemDto(
    Guid Id,
    string Name,
    int StockQuantity,
    int ReorderLevel);