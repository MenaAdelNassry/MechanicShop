using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.Workorders.Billing.Errors;

public static class PaymentErrors
{
    public static readonly Error AmountMustBePositive =
        Error.Validation("Payment.AmountMustBePositive", "Payment amount must be greater than zero.");

    public static readonly Error InvoiceAlreadyPaid =
        Error.Conflict("Payment.InvoiceAlreadyPaid", "The invoice has already been fully paid.");

    public static readonly Error CannotPayRefundedInvoice =
        Error.Conflict("Payment.CannotPayRefundedInvoice", "Cannot record payments on a refunded invoice.");

    public static Error PaymentExceedsRemaining(decimal remaining) =>
        Error.Validation("Payment.ExceedsRemaining", $"Payment amount exceeds the remaining balance of {remaining:C}.");

    public static readonly Error UserUnauthenticated =
        Error.Unauthorized("Payment.UserUnauthenticated", "User identity is required to record the payment.");

    public static readonly Error PaymentNotFound =
        Error.NotFound("Payment.NotFound", "Payment not found.");

    public static readonly Error ReferenceRequiredForCard =
        Error.Validation("Payment.ReferenceRequiredForCard", "A transaction reference or receipt number is required for card and bank payments.");
}