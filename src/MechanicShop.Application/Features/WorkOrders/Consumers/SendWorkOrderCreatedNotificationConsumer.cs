using MassTransit;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Common.Settings;
using MechanicShop.Application.Features.WorkOrders.EventHandlers;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MechanicShop.Application.Features.WorkOrders.Consumers;

public sealed class SendWorkOrderCreatedNotificationConsumer(
    ISmsSender smsSender,
    IEmailSender emailSender,
    IEmailTemplateRenderer templateRenderer,
    IAppDbContext context,
    IOptions<AppSettings> appSettings,
    ILogger<SendWorkOrderCreatedNotificationConsumer> logger)
    : IConsumer<WorkOrderCreatedIntegrationEvent>
{
    private readonly AppSettings _settings = appSettings.Value;

    public async Task Consume(ConsumeContext<WorkOrderCreatedIntegrationEvent> consumeContext)
    {
        var message = consumeContext.Message;
        var ct = consumeContext.CancellationToken;

        var workOrder = await context.WorkOrders
            .Include(w => w.Vehicle!)
                .ThenInclude(v => v.Customer)
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == message.WorkOrderId, ct);

        if (workOrder?.Vehicle?.Customer is null)
        {
            logger.LogWarning("Notification skipped: Customer not found for WorkOrder '{WorkOrderId}'.", message.WorkOrderId);
            return;
        }

        var customer = workOrder.Vehicle.Customer;
        var customerName = customer.Name?.FullName ?? "Customer";
        var trackingUrl = $"{_settings.ClientAppBaseUrl.TrimEnd('/')}/track/{message.TrackingToken}";
        var vehicleInfo = $"{workOrder.Vehicle.Make} {workOrder.Vehicle.Model}".Trim();

        // 1. SMS
        if (!string.IsNullOrWhiteSpace(customer.PhoneNumber?.Value))
        {
            var sms = $"MechanicShop: Track live repairs for your {vehicleInfo}: {trackingUrl}";
            await smsSender.SendSmsAsync(customer.PhoneNumber.Value, sms, ct);
        }

        // 2. Email
        if (!string.IsNullOrWhiteSpace(customer.Email?.Value))
        {
            var html = await templateRenderer.RenderAsync(
                title: "Vehicle Repair Live Tracking",
                recipientName: customerName,
                messageHtml: $"We have checked in your <strong>{vehicleInfo}</strong>. You can follow the live status of your vehicle directly from our tracking portal.",
                buttonText: "Track Vehicle Live",
                buttonUrl: trackingUrl,
                ct: ct);

            await emailSender.SendEmailAsync(
                customer.Email.Value,
                "🔧 MechanicShop — Vehicle Live Tracking",
                html,
                ct: ct);
        }
    }
}