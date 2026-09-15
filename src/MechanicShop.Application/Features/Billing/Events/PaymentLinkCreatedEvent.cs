namespace MechanicShop.Application.Features.Billing.Events;

public sealed record PaymentLinkCreatedEvent(
    Guid InvoiceId,
    string CustomerName,
    string? CustomerEmail,
    string? CustomerPhoneNumber,
    decimal Amount,
    string PaymentUrl
);