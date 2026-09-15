namespace MechanicShop.Application.Features.Reports.Dtos;

public sealed record PartUsageItemDto
{
    public Guid InventoryItemId { get; init; }
    public string PartName { get; init; } = string.Empty;
    public int QuantityUsed { get; init; }
    public decimal AverageUnitCost { get; init; }
    public decimal TotalCost { get; init; }
    public int WorkOrdersCount { get; init; }
    public int CurrentStock { get; init; }
    public int ReorderLevel { get; init; }
    public bool IsLowStock { get; init; }
}
