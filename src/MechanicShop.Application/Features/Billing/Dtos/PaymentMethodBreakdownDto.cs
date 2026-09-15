using MechanicShop.Domain.Workorders.Billing.Enums;

namespace MechanicShop.Application.Features.Billing.Dtos;

public sealed record PaymentMethodBreakdownDto
{
    public PaymentMethod Method { get; init; }
    public int TransactionsCount { get; init; }
    public decimal TotalAmount { get; init; }
}
