using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Application.Features.Billing.Mappers;
using MechanicShop.Application.Metrices;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders;
using MechanicShop.Domain.Workorders.Billing;
using MechanicShop.Domain.Workorders.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Billing.Commands.IssueInvoice;

public class IssueInvoiceCommandHandler(
    ILogger<IssueInvoiceCommandHandler> logger,
    IAppDbContext context,
    TimeProvider datetime
    )
    : IRequestHandler<IssueInvoiceCommand, Result<InvoiceDto>>
{
    public async Task<Result<InvoiceDto>> Handle(IssueInvoiceCommand command, CancellationToken ct)
    {
        var workOrder = await context.WorkOrders
                .Include(w => w.Vehicle)
                    .ThenInclude(v => v!.Customer)
                .Include(w => w.RepairTasks)
                    .ThenInclude(rt => rt.Parts)
                .FirstOrDefaultAsync(w => w.Id == command.WorkOrderId, ct);

        if (workOrder is null)
        {
            logger.LogWarning("Invoice issuance failed. WorkOrder {WorkOrderId} not found.", command.WorkOrderId);

            return ApplicationErrors.WorkOrders.NotFound;
        }

        if (workOrder.State != WorkOrderState.Completed)
        {
            logger.LogWarning("Invoice issuance rejected. WorkOrder {WorkOrderId} is not in completed.", command.WorkOrderId);

            return ApplicationErrors.Invoices.WorkOrderMustBeCompletedForInvoicing;
        }

        var invoiceExists = await context.Invoices
        .AnyAsync(i => i.WorkOrderId == command.WorkOrderId, ct);

        if (invoiceExists)
        {
            logger.LogWarning("Invoice issuance rejected. WorkOrder {WorkOrderId} already has an issued invoice.", command.WorkOrderId);
            return ApplicationErrors.Invoices.InvoiceAlreadyIssuedForWorkOrder;
        }

        Guid invoiceId = Guid.CreateVersion7();

        var lineItems = new List<InvoiceLineItem>();

        var lineNumber = 1;

        foreach (var task in workOrder.RepairTasks)
        {
            // 1. A line for the “Labor” attribute of the Task
            var lineItemResult = InvoiceLineItem.Create(
                invoiceId: invoiceId,
                lineNumber: lineNumber++,
                description: $"Service: {task.Name}",
                quantity: 1,
                unitPrice: task.LaborCost);

            if(lineItemResult.IsError)
            {
                return lineItemResult.Errors;
            }

            lineItems.Add(lineItemResult.Value);

            // 2. One line for each part used in this task
            foreach (var part in task.Parts)
            {
                var partItemResult = InvoiceLineItem.Create(
                    invoiceId: invoiceId,
                    lineNumber: lineNumber++,
                    description: $"Spare part: {part.Name}",
                    quantity: part.Quantity,
                    unitPrice: part.Cost);

                if (partItemResult.IsError)
                {
                    return partItemResult.Errors;
                }

                lineItems.Add(partItemResult.Value);
            }
        }

        var discountAmount = command.DiscountAmount ?? 0m;

        var createInvoiceResult = Invoice.Create(
            id: invoiceId,
            workOrderId: workOrder.Id,
            items: lineItems,
            discountAmount: discountAmount,
            datetime: datetime);

        if (createInvoiceResult.IsError)
        {
            logger.LogWarning(
                 "Invoice creation failed for WorkOrderId: {WorkOrderId}. Errors: {@Errors}",
                 command.WorkOrderId,
                 createInvoiceResult.Errors);

            return createInvoiceResult.Errors;
        }

        var invoice = createInvoiceResult.Value;

        context.Invoices.Add(invoice);

        await context.SaveChangesAsync(ct);

        MechanicShopMetrics.InvoicesIssued.Add(1);

        logger.LogInformation("Invoice {InvoiceId} issued for WorkOrder {WorkOrderId}.", invoice.Id, workOrder.Id);

        return invoice.ToDto();
    }
}