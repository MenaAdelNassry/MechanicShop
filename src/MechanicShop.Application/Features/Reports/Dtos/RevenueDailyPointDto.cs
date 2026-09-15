namespace MechanicShop.Application.Features.Reports.Dtos;

public sealed record RevenueDailyPointDto
{
    public DateOnly Date { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Discounts { get; init; }
    public decimal Taxes { get; init; }
    public int PaidInvoicesCount { get; init; }
}