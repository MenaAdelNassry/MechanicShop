namespace MechanicShop.Domain.Workorders.Billing;

public enum InvoiceStatus
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Refunded = 4
}