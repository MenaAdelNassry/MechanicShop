using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Application.Common.Interfaces;

public interface IStripePaymentService
{
    Task<Result<string>> CreateCheckoutSessionAsync(
        Guid invoiceId,
        Guid paymentId,
        decimal amount,
        string currency,
        string successUrl,
        string cancelUrl,
        CancellationToken ct = default);

    Result<StripeWebhookResult> ProcessWebhookEvent(string payload, string signatureHeader);
}

public sealed record StripeWebhookResult(
    string EventType,
    Guid InvoiceId,
    Guid PaymentId,
    string TransactionReference,
    decimal AmountPaid
);