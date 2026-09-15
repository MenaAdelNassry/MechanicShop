using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Infrastructure.Settings;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Stripe;
using Stripe.Checkout;

namespace MechanicShop.Infrastructure.Payments.Stripe;

public sealed class StripePaymentService : IStripePaymentService
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(IOptions<StripeSettings> options, ILogger<StripePaymentService> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<Result<string>> CreateCheckoutSessionAsync(
        Guid invoiceId,
        Guid paymentId,
        decimal amount,
        string currency,
        string successUrl,
        string cancelUrl,
        CancellationToken ct = default)
    {
        try
        {
            // Convert the amount to piastres/cents
            var unitAmountInCents = (long)Math.Round(amount * 100, MidpointRounding.AwayFromZero);

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = ["card"],
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                ClientReferenceId = invoiceId.ToString(),
                Metadata = new Dictionary<string, string>
                {
                    ["InvoiceId"] = invoiceId.ToString(),
                    ["PaymentId"] = paymentId.ToString()
                },
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency.ToLowerInvariant(),
                            UnitAmount = unitAmountInCents,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Invoice #{invoiceId.ToString()[..8]} Payment",
                                Description = "Auto repair services invoice settlement"
                            }
                        },
                        Quantity = 1
                    }
                ]
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options, cancellationToken: ct);

            return session.Url;
        }
        catch (StripeException ex)
        {
            return Error.Failure("Stripe.SessionCreationFailed", ex.Message);
        }
    }

    public Result<StripeWebhookResult> ProcessWebhookEvent(string payload, string signatureHeader)
    {
        try
        {
            // Verify the signature within the Infrastructure
            var stripeEvent = EventUtility.ConstructEvent(
                payload,
                signatureHeader,
                _settings.WebhookSecret,
                throwOnApiVersionMismatch: false);

            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted &&
                stripeEvent.Data.Object is Session session)
            {
                if (!Guid.TryParse(session.ClientReferenceId, out var invoiceId))
                {
                    return Error.Validation("Stripe.InvalidInvoiceId", "Missing or invalid ClientReferenceId.");
                }

                if(!Guid.TryParse(session.Metadata["PaymentId"], out var paymentId))
                {
                    return Error.Validation("Stripe.InvalidPaymentId", "Missing or invalid PaymentId in metadata.");
                }

                var transactionRef = session.PaymentIntentId ?? session.Id;
                var amountPaid = (decimal)(session.AmountTotal ?? 0) / 100m;

                return new StripeWebhookResult(
                    EventType: stripeEvent.Type,
                    PaymentId: paymentId,
                    InvoiceId: invoiceId,
                    TransactionReference: transactionRef,
                    AmountPaid: amountPaid);
            }

            return new StripeWebhookResult(stripeEvent.Type, Guid.Empty, Guid.Empty, string.Empty, 0);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature verification failed.");
            return Error.Failure("Stripe.InvalidSignature", "Webhook signature verification failed.");
        }
    }
}