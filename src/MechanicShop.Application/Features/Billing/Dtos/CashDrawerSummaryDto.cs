namespace MechanicShop.Application.Features.Billing.Dtos;

public sealed record CashDrawerSummaryDto
{
    public DateOnly Date { get; init; }
    public decimal GrandTotalCollected { get; init; }
    public int TotalTransactionsCount { get; init; }
    public IReadOnlyList<PaymentMethodBreakdownDto> BreakdownByMethod { get; init; } = [];
}