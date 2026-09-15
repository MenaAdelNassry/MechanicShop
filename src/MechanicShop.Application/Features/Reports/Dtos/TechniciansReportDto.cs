namespace MechanicShop.Application.Features.Reports.Dtos;

public sealed record TechniciansReportDto
{
    public DateOnly FromDate { get; init; }
    public DateOnly ToDate { get; init; }
    public int TotalCompletedOrders { get; init; }
    public double AverageEfficiencyPercentage { get; init; }
    public decimal TotalLaborRevenue { get; init; }
    public IReadOnlyList<TechnicianProductivitySummaryDto> Technicians { get; init; } = [];
}