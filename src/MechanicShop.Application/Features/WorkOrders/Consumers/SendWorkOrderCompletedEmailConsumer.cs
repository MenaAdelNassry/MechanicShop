using MassTransit;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.EventHandlers;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Consumers;

public sealed class SendWorkOrderCompletedEmailConsumer(
    IEmailSender emailSender,
    IEmailTemplateRenderer templateRenderer,
    IAppDbContext context,
    ILogger<SendWorkOrderCompletedEmailConsumer> logger)
    : IConsumer<WorkOrderCompletedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<WorkOrderCompletedIntegrationEvent> consumeContext)
    {
        var message = consumeContext.Message;
        var ct = consumeContext.CancellationToken;

        var workOrder = await context.WorkOrders
            .Include(w => w.Vehicle!)
                .ThenInclude(v => v.Customer)
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == message.WorkOrderId, ct);

        if (workOrder is null)
        {
            logger.LogError("Email Dispatch Cancelled: WorkOrder '{WorkOrderId}' does not exist.", message.WorkOrderId);
            return;
        }

        var customer = workOrder.Vehicle?.Customer;
        var customerEmail = customer?.Email?.Value;

        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            logger.LogWarning("Email Dispatch Skipped: Customer email is missing for WorkOrder '{WorkOrderId}'.", message.WorkOrderId);
            return;
        }

        var customerName = customer?.Name?.FullName ?? "Valued Customer";
        var vehicleInfo = $"{workOrder.Vehicle?.Make} {workOrder.Vehicle?.Model} ({workOrder.Vehicle?.LicensePlate})";

        var subject = "🚗 MechanicShop — Service Completed & Ready for Pickup";

        var messageContent = $@"
            Great news! The service on your vehicle <strong>{vehicleInfo}</strong> has been successfully completed.<br/><br/>
            You may collect your vehicle from our service center at your earliest convenience.";

        var htmlBody = await templateRenderer.RenderAsync(
            title: "Your Vehicle is Ready!",
            recipientName: customerName,
            messageHtml: messageContent,
            ct: ct);

        var textBody = $"Hello {customerName},\n\n" +
                       $"Great news! The service on your vehicle {vehicleInfo} has been successfully completed.\n" +
                       $"You may collect it from the shop at your earliest convenience.\n\n" +
                       $"Thank you for choosing MechanicShop!";

        await emailSender.SendEmailAsync(customerEmail, subject, htmlBody, textBody, ct);
    }
}