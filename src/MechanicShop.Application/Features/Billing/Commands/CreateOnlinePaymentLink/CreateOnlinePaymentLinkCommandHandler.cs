using MassTransit;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Common.Settings;
using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Application.Features.Billing.Events;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Billing;
using MechanicShop.Domain.Workorders.Billing.Enums;
using MechanicShop.Domain.Workorders.Billing.Errors;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MechanicShop.Application.Features.Billing.Commands.CreateOnlinePaymentLink;

public sealed class CreateOnlinePaymentLinkCommandHandler(
    IAppDbContext context,
    IStripePaymentService stripeService,
    IPublishEndpoint publishEndpoint,
    IOptions<AppSettings> appSettings,
    TimeProvider timeProvider
) : IRequestHandler<CreateOnlinePaymentLinkCommand, Result<CreatePaymentLinkDto>>
{
    private readonly AppSettings _settings = appSettings.Value;

    public async Task<Result<CreatePaymentLinkDto>> Handle(CreateOnlinePaymentLinkCommand request, CancellationToken ct)
    {
        // 1. Bring the invoice along with the payments to calculate the remaining balance.
        var invoice = await context.Invoices
            .Include(i => i.Payments)
            .Include(i => i.WorkOrder)
                .ThenInclude(w => w!.Vehicle)
                    .ThenInclude(v => v!.Customer)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct);

        if (invoice is null)
        {
            return InvoiceErrors.InvoiceNotFoundById(request.InvoiceId);
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            return PaymentErrors.InvoiceAlreadyPaid;
        }

        if (invoice.RemainingAmount <= 0)
        {
            return PaymentErrors.AmountMustBePositive;
        }

        // 2. Request a payment link from the Stripe service for the remaining amount
        var paymentId = Guid.CreateVersion7();

        var checkoutUrlResult = await stripeService.CreateCheckoutSessionAsync(
            invoiceId: invoice.Id,
            paymentId: paymentId,
            amount: invoice.RemainingAmount,
            currency: _settings.DefaultCurrency,
            successUrl: request.SuccessUrl,
            cancelUrl: request.CancelUrl,
            ct: ct);

        if (checkoutUrlResult.IsError)
        {
            return checkoutUrlResult.Errors;
        }

        var checkoutUrl = checkoutUrlResult.Value;

        // 3. Create a pending payment record in the database
        var pendingPaymentResult = Payment.Create(
            id: paymentId,
            invoiceId: invoice.Id,
            amount: invoice.RemainingAmount,
            method: PaymentMethod.OnlineGateway,
            status: PaymentStatus.Pending,
            receivedByUserId: null,
            transactionReference: null,
            timeProvider: timeProvider);

        if (pendingPaymentResult.IsError)
        {
            return pendingPaymentResult.Errors;
        }

        context.Payments.Add(pendingPaymentResult.Value);

        // 4. Sending SMS and Email Msg
        var customer = invoice.WorkOrder?.Vehicle?.Customer;
        await publishEndpoint.Publish(
            new PaymentLinkCreatedEvent(
            InvoiceId: invoice.Id,
            CustomerName: customer?.Name.FullName ?? string.Empty,
            CustomerEmail: customer?.Email.Value ?? string.Empty,
            CustomerPhoneNumber: customer?.PhoneNumber.Value ?? string.Empty,
            Amount: invoice.RemainingAmount,
            PaymentUrl: checkoutUrl), ct);

        await context.SaveChangesAsync(ct);

        return new CreatePaymentLinkDto(checkoutUrl);
    }
}