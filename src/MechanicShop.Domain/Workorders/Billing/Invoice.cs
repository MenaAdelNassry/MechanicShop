using MechanicShop.Domain.Common;
using MechanicShop.Domain.Common.Constants;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Billing.Enums;
using MechanicShop.Domain.Workorders.Billing.Errors;

namespace MechanicShop.Domain.Workorders.Billing;

public sealed class Invoice : AuditableEntity
{
    public Guid WorkOrderId { get; }
    public DateTimeOffset? PaidAt { get; private set; }
    public decimal TaxRateAtIssuance { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public DateTimeOffset IssuedAtUtc { get; private set; }

    public decimal Subtotal => LineItems.Sum(x => x.LineTotal);
    public decimal TaxAmount => Subtotal * TaxRateAtIssuance;
    public decimal Total => Subtotal - DiscountAmount + TaxAmount;

    public WorkOrder? WorkOrder { get; set; }

    private readonly List<InvoiceLineItem> _lineItems = [];
    public IReadOnlyList<InvoiceLineItem> LineItems => _lineItems;

    private readonly List<Payment> _payments = new();
    public IReadOnlyList<Payment> Payments => _payments;
    public InvoiceStatus Status { get; private set; }
    public decimal TotalPaid => _payments
    .Where(p => p.Status == PaymentStatus.Completed)
    .Sum(p => p.Amount);

    public decimal RemainingAmount => Math.Max(0, Total - TotalPaid);
    private Invoice()
    { }

    private Invoice(
        Guid id,
        Guid workOrderId,
        DateTimeOffset issuedAt,
        List<InvoiceLineItem> lineItems,
        decimal discountAmount)
        : base(id)
    {
        WorkOrderId = workOrderId;
        IssuedAtUtc = issuedAt;
        DiscountAmount = discountAmount;
        Status = InvoiceStatus.Unpaid;
        _lineItems = lineItems;
        TaxRateAtIssuance = MechanicShopConstants.TaxRate;
    }

    public static Result<Invoice> Create(
        Guid id,
        Guid workOrderId,
        List<InvoiceLineItem> items,
        decimal discountAmount,
        TimeProvider datetime)
    {
        if (workOrderId == Guid.Empty)
        {
            return InvoiceErrors.WorkOrderIdInvalid;
        }

        if (items is null || items.Count == 0)
        {
            return InvoiceErrors.LineItemsEmpty;
        }

        if (discountAmount < 0)
        {
            return InvoiceErrors.DiscountNegative;
        }

        var subtotal = items.Sum(x => x.LineTotal);
        if (discountAmount > subtotal)
        {
            return InvoiceErrors.DiscountExceedsSubtotal;
        }

        return new Invoice(id, workOrderId, datetime.GetUtcNow(), items, discountAmount);
    }

    public Result<Updated> ApplyDiscount(decimal discountAmount)
    {
        if (Status != InvoiceStatus.Unpaid)
        {
            return InvoiceErrors.InvoiceLocked;
        }

        if (discountAmount < 0)
        {
            return InvoiceErrors.DiscountNegative;
        }

        if (discountAmount > Subtotal)
        {
            return InvoiceErrors.DiscountExceedsSubtotal;
        }

        DiscountAmount = discountAmount;

        return Result.Updated;
    }

    public Result<Updated> MarkAsPaid(TimeProvider timeProvider)
    {
        if (Status != InvoiceStatus.Unpaid && Status != InvoiceStatus.PartiallyPaid)
        {
            return InvoiceErrors.InvoiceLocked;
        }

        Status = InvoiceStatus.Paid;
        PaidAt = timeProvider.GetUtcNow();

        return Result.Updated;
    }

    public Result<Updated> Refund()
    {
        if (Status != InvoiceStatus.Paid && Status != InvoiceStatus.PartiallyPaid)
        {
            return InvoiceErrors.CannotRefundUnpaidInvoice;
        }

        Status = InvoiceStatus.Refunded;

        return Result.Updated;
    }

    public Result<Payment> RecordPayment(
        Guid paymentId,
        decimal amount,
        PaymentMethod method,
        Guid? receivedByUserId,
        string? transactionReference,
        TimeProvider timeProvider)
    {
        if (Status == InvoiceStatus.Paid)
        {
            return PaymentErrors.InvoiceAlreadyPaid;
        }

        if (Status == InvoiceStatus.Refunded)
        {
            return PaymentErrors.CannotPayRefundedInvoice;
        }

        if (amount <= 0)
        {
            return PaymentErrors.AmountMustBePositive;
        }

        if (amount > RemainingAmount)
        {
            return PaymentErrors.PaymentExceedsRemaining(RemainingAmount);
        }

        var paymentResult = Payment.Create(
            paymentId,
            Id,
            amount,
            method,
            PaymentStatus.Completed,
            receivedByUserId,
            transactionReference,
            timeProvider);

        if (paymentResult.IsError)
        {
            return paymentResult.Errors;
        }

        var payment = paymentResult.Value;
        _payments.Add(payment);

        if (RemainingAmount == 0)
        {
            Status = InvoiceStatus.Paid;
            PaidAt = timeProvider.GetUtcNow();
        }
        else
        {
            Status = InvoiceStatus.PartiallyPaid;
        }

        return payment;
    }

    public Result<Updated> CompleteOnlinePayment(Guid paymentId, string transactionReference, TimeProvider timeProvider)
    {
        if (Status == InvoiceStatus.Refunded)
        {
            return InvoiceErrors.InvoiceLocked;
        }

        var payment = _payments.FirstOrDefault(p => p.Id == paymentId);
        if (payment is null)
        {
            return PaymentErrors.PaymentNotFound;
        }

        var markResult = payment.MarkAsCompleted(transactionReference, timeProvider);
        if (markResult.IsError)
        {
            return markResult.Errors;
        }

        if (RemainingAmount <= 0)
        {
            MarkAsPaid(timeProvider);
        }
        else if (TotalPaid > 0)
        {
            Status = InvoiceStatus.PartiallyPaid;
        }

        if (Status == InvoiceStatus.Paid)
        {
            foreach (var pendingPayment in _payments.Where(p => p.Status == PaymentStatus.Pending))
            {
                pendingPayment.MarkAsFailed();
            }
        }

        return Result.Updated;
    }
}