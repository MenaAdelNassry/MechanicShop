using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.Workorders.Billing.Errors;

public static class InvoiceErrors
{
    public static readonly Error WorkOrderIdInvalid = Error.Validation(
        code: "Invoice.WorkOrderId.Invalid",
        description: "WorkOrderId is invalid");

    public static readonly Error LineItemsEmpty = Error.Validation(
        code: "Invoice.LineItems.Empty",
        description: "Invoice must have line items");

    public static readonly Error InvoiceLocked = Error.Validation(
        code: "Invoice.Locked",
        description: "Invoice is locked");

    public static readonly Error DiscountNegative = Error.Validation(
        code: "Invoice.Discount.Negative",
        description: "Discount cannot be negative");

    public static readonly Error DiscountExceedsSubtotal = Error.Validation(
        code: "Invoice.Discount.ExceedsSubtotal",
        description: "Discount exceeds subtotal");

    public static readonly Error CannotRefundUnpaidInvoice = Error.Validation(
        code: "Invoice.Refund.Unpaid",
        description: "Cannot refund an unpaid invoice");

    public static readonly Error InvoiceNotFound = Error.NotFound(
        code: "Invoice.NotFound",
        description: "Invoice not found");

    public static Error InvoiceNotFoundById(Guid invoiceId) => Error.NotFound(
        code: "Invoice.NotFound",
        description: $"Invoice with ID '{invoiceId}' not found");

    public static readonly Error InvoiceAlreadyPaid = Error.Validation(
        code: "Invoice.AlreadyPaid",
        description: "Invoice is already paid");
}