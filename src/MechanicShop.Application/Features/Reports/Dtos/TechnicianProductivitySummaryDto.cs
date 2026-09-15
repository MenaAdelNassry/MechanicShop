namespace MechanicShop.Application.Features.Reports.Dtos;

public sealed record TechnicianProductivitySummaryDto
{
    public Guid LaborId { get; init; }
    public string LaborName { get; init; } = string.Empty;
    public int CompletedOrdersCount { get; init; }
    public int CompletedTasksCount { get; init; }
    public double TotalEstimatedHours { get; init; }
    public double TotalActualHours { get; init; }
    public double EfficiencyPercentage { get; init; }
    public decimal TotalLaborRevenueGenerated { get; init; }
}