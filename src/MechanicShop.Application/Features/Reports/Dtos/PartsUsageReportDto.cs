namespace MechanicShop.Application.Features.Reports.Dtos;

public sealed record PartsUsageReportDto
{
    public DateOnly FromDate { get; init; }
    public DateOnly ToDate { get; init; }
    public int TotalPartsQuantityUsed { get; init; }
    public decimal TotalPartsCost { get; init; }
    public int DistinctPartsCount { get; init; }
    public IReadOnlyList<PartUsageItemDto> Parts { get; init; } = [];
}