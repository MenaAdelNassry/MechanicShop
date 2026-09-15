using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Billing.Errors;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Billing.Commands.RecordPayment;

public sealed class RecordPaymentCommandHandler(
    IAppDbContext context,
    IUser currentUser,
    TimeProvider timeProvider
) : IRequestHandler<RecordPaymentCommand, Result<PaymentDto>>
{
    public async Task<Result<PaymentDto>> Handle(RecordPaymentCommand request, CancellationToken ct)
    {
        // 1. Verify the identity of the current employee registered with the token.
        Guid? currentUserId = null;
        if (Guid.TryParse(currentUser.Id, out var parsedGuid))
        {
            currentUserId = parsedGuid;
        }

        // 2. Retrieve the invoice including the list of previous payments to accurately calculate the remaining balance.
        var invoice = await context.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, ct);

        if (invoice is null)
        {
            return InvoiceErrors.InvoiceNotFoundById(request.InvoiceId);
        }

        // 3. Call the domain method to record the payment and update the invoice status.
        var paymentId = Guid.CreateVersion7();
        var paymentResult = invoice.RecordPayment(
            paymentId: paymentId,
            amount: request.Amount,
            method: request.Method,
            receivedByUserId: currentUserId,
            transactionReference: request.TransactionReference,
            timeProvider: timeProvider);

        if (paymentResult.IsError)
        {
            return paymentResult.Errors;
        }

        var payment = paymentResult.Value;

        // 4. Save changes to the database
        await context.SaveChangesAsync(ct);

        // 5. Return the DTO
        return new PaymentDto
        {
            Id = payment.Id,
            InvoiceId = payment.InvoiceId,
            Amount = payment.Amount,
            Method = payment.Method,
            Status = payment.Status,
            TransactionReference = payment.TransactionReference,
            ReceivedByUserId = payment.ReceivedByUserId,
            PaidAtUtc = payment.PaidAtUtc
        };
    }
}