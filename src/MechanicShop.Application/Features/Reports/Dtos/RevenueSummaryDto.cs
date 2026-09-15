namespace MechanicShop.Application.Features.Reports.Dtos;

public sealed record RevenueSummaryDto
{
    public DateOnly FromDate { get; init; }
    public DateOnly ToDate { get; init; }
    public decimal TotalNetRevenue { get; init; }
    public decimal TotalSubtotal { get; init; }
    public decimal TotalDiscounts { get; init; }
    public decimal TotalTaxes { get; init; }
    public int TotalPaidInvoices { get; init; }
    public decimal AverageInvoiceValue { get; init; }
    public IReadOnlyList<RevenueDailyPointDto> DailyBreakdown { get; init; } = [];
}