using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Billing;
using MechanicShop.Domain.Workorders.Billing.Enums;
using MechanicShop.Domain.Workorders.Billing.Errors;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Billing.Commands.ProcessStripeWebhook;

public sealed class ProcessStripeWebhookCommandHandler(
    ILogger<ProcessStripeWebhookCommandHandler> logger,
    IAppDbContext context,
    IStripePaymentService stripePaymentService,
    TimeProvider timeProvider
) : IRequestHandler<ProcessStripeWebhookCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ProcessStripeWebhookCommand request, CancellationToken ct)
    {
        // 1. handling in Infrastructure
        var webhookResult = stripePaymentService.ProcessWebhookEvent(request.Payload, request.SignatureHeader);

        if (webhookResult.IsError)
        {
            return webhookResult.Errors;
        }

        var resultData = webhookResult.Value;

        // 2. if the event is payment completion
        if (resultData.InvoiceId != Guid.Empty)
        {
            var invoice = await context.Invoices
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == resultData.InvoiceId, ct);

            if (invoice is null)
            {
                logger.LogWarning("Invoice {InvoiceId} referenced in Stripe session was not found.", resultData.InvoiceId);
                return InvoiceErrors.InvoiceNotFound;
            }

            var pendingPayment = invoice.Payments
                .FirstOrDefault(p => p.Id == resultData.PaymentId);

            if(pendingPayment is not null && pendingPayment.Status == PaymentStatus.Completed)
            {
                logger.LogInformation("Invoice {InvoiceId} already has a completed payment with ID {PaymentId}.", invoice.Id, pendingPayment.Id);
                return Result.Success;
            }

            if (pendingPayment is not null)
            {
                invoice.CompleteOnlinePayment(pendingPayment.Id, resultData.TransactionReference, timeProvider);
            }
            else
            {
                invoice.RecordPayment(
                    paymentId: Guid.CreateVersion7(),
                    amount: resultData.AmountPaid,
                    method: PaymentMethod.OnlineGateway,
                    receivedByUserId: null,
                    transactionReference: resultData.TransactionReference,
                    timeProvider: timeProvider);
            }

            await context.SaveChangesAsync(ct);

            logger.LogInformation("Invoice {InvoiceId} successfully updated via Stripe Webhook.", invoice.Id);
        }

        return Result.Success;
    }
}