using MechanicShop.Domain.Common;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Billing.Enums;
using MechanicShop.Domain.Workorders.Billing.Errors;

namespace MechanicShop.Domain.Workorders.Billing;

public sealed class Payment : Entity
{
    public Guid InvoiceId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? TransactionReference { get; private set; }
    public Guid? ReceivedByUserId { get; private set; }
    public DateTimeOffset? PaidAtUtc { get; private set; }

    public Invoice? Invoice { get; private set; }

    private Payment()
    { }

    private Payment(
        Guid id,
        Guid invoiceId,
        decimal amount,
        PaymentMethod method,
        PaymentStatus status,
        Guid? receivedByUserId,
        string? transactionReference,
        DateTimeOffset? paidAtUtc)
        : base(id)
    {
        InvoiceId = invoiceId;
        Amount = amount;
        Method = method;
        Status = status;
        ReceivedByUserId = receivedByUserId;
        TransactionReference = transactionReference;
        PaidAtUtc = paidAtUtc;
    }

    public static Result<Payment> Create(
        Guid id,
        Guid invoiceId,
        decimal amount,
        PaymentMethod method,
        PaymentStatus status,
        Guid? receivedByUserId,
        string? transactionReference,
        TimeProvider timeProvider)
    {
        if (id == Guid.Empty)
        {
            return Error.Validation("Payment.IdRequired", "Payment identifier is required.");
        }

        if (invoiceId == Guid.Empty)
        {
            return Error.Validation("Payment.InvoiceIdRequired", "Invoice identifier is required.");
        }

        if (amount <= 0)
        {
            return PaymentErrors.AmountMustBePositive;
        }

        DateTimeOffset? paidAt = status == PaymentStatus.Completed
            ? timeProvider.GetUtcNow()
            : null;

        return new Payment(
            id,
            invoiceId,
            amount,
            method,
            status,
            receivedByUserId,
            transactionReference?.Trim(),
            paidAt);
    }

    public Result<Updated> MarkAsCompleted(string transactionReference, TimeProvider timeProvider)
    {
        if(Status == PaymentStatus.Completed)
        {
            return Error.Validation("Payment.AlreadyCompleted", "Payment is already completed.");
        }

        Status = PaymentStatus.Completed;
        TransactionReference = transactionReference.Trim();
        PaidAtUtc = timeProvider.GetUtcNow();

        return Result.Updated;
    }

    public Result<Updated> MarkAsFailed()
    {
        Status = PaymentStatus.Failed;
        return Result.Updated;
    }
}