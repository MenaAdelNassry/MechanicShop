using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Billing;

namespace MechanicShop.Tests.Common.Billing;

public static class InvoiceFactory
{
    public static Result<Invoice> CreateInvoice(
        Guid? id = null,
        Guid? workOrderId = null,
        List<InvoiceLineItem>? items = null,
        decimal? discount = null,
        TimeProvider? timeProvider = null)
    {
        return Invoice.Create(
            id ?? Guid.CreateVersion7(),
            workOrderId ?? Guid.CreateVersion7(),
            items ?? [InvoiceLineItem.Create(Guid.CreateVersion7(), 1, "Oil Change", 2, 50).Value],
            discount ?? 0,
            timeProvider ?? TimeProvider.System);
    }
}