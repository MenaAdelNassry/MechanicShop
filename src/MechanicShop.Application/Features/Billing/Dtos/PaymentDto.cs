using MechanicShop.Domain.Workorders.Billing.Enums;

namespace MechanicShop.Application.Features.Billing.Dtos;

public sealed record PaymentDto
{
    public Guid Id { get; init; }
    public Guid InvoiceId { get; init; }
    public decimal Amount { get; init; }
    public PaymentMethod Method { get; init; }
    public PaymentStatus Status { get; init; }
    public string? TransactionReference { get; init; }
    public Guid? ReceivedByUserId { get; init; }
    public DateTimeOffset? PaidAtUtc { get; init; }
}