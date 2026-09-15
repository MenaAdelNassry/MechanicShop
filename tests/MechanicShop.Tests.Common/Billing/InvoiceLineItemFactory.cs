using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Billing;

namespace MechanicShop.Tests.Common.Billing;

public static class InvoiceLineItemFactory
{
    public static Result<InvoiceLineItem> CreateInvoiceLineItem(
        Guid? id = null,
        int? lineNumber = null,
        string? description = null,
        int? quantity = null,
        decimal? unitPrice = null)
    {
        return InvoiceLineItem.Create(
            id ?? Guid.CreateVersion7(),
            lineNumber ?? 1,
            description ?? "some invoice line",
            quantity ?? 1,
            unitPrice ?? 100m);
    }
}