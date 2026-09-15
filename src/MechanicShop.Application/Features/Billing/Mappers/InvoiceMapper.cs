using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Application.Features.Customers.Mappers;
using MechanicShop.Domain.Workorders.Billing;

namespace MechanicShop.Application.Features.Billing.Mappers;

public static class InvoiceMapper
{
    public static InvoiceDto ToDto(this Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return new InvoiceDto
        {
            InvoiceId = invoice.Id,
            WorkOrderId = invoice.WorkOrderId,
            Customer = invoice.WorkOrder?.Vehicle?.Customer?.ToDto(),
            Vehicle = invoice.WorkOrder?.Vehicle?.ToDto(),
            IssuedAtUtc = invoice.IssuedAtUtc,
            Subtotal = invoice.Subtotal,
            TaxAmount = invoice.TaxAmount,
            DiscountAmount = invoice.DiscountAmount,
            Total = invoice.Total,
            PaymentStatus = invoice.Status.ToString(),
            TotalPaid = invoice.TotalPaid,
            RemainingAmount = invoice.RemainingAmount,
            Items = [.. invoice.LineItems.Select(x => x.ToDto())],
            Payments = [.. invoice.Payments.Select(p => p.ToDto())]
        };
    }

    public static List<InvoiceDto> ToDtos(this IEnumerable<Invoice> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        return [.. entities.Select(e => e.ToDto())];
    }

    public static InvoiceLineItemDto ToDto(this InvoiceLineItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new InvoiceLineItemDto
        {
            InvoiceId = item.InvoiceId,
            LineNumber = item.LineNumber,
            Description = item.Description,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            LineTotal = item.LineTotal
        };
    }

    public static List<InvoiceLineItemDto> ToDtos(this IEnumerable<InvoiceLineItem> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        return [.. entities.Select(e => e.ToDto())];
    }

    public static PaymentDto ToDto(this Payment payment)
    {
        ArgumentNullException.ThrowIfNull(payment);
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