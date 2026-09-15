using MassTransit;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Inventory.EventHandlers;
using MechanicShop.Application.Features.Inventory.Notifications;
using MechanicShop.Infrastructure.Settings;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MechanicShop.Application.Features.Inventory.Consumers;

public sealed class SendLowStockNotificationHandler(
    IEmailSender emailSender,
    IEmailTemplateRenderer templateRenderer,
    IOptions<EmailSettings> emailOptions,
    IAppDbContext context,
    ILogger<SendLowStockNotificationHandler> logger)
    : IConsumer<InventoryItemLowStockIntegrationEvent>
{
    private readonly EmailSettings _emailSettings = emailOptions.Value;

    public async Task Consume(ConsumeContext<InventoryItemLowStockIntegrationEvent> contextConsumer)
    {
        var message = contextConsumer.Message;
        var ct = contextConsumer.CancellationToken;

        var item = await context.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == message.InventoryItemId, ct);

        if (item is null)
        {
            logger.LogWarning("Low stock notification skipped: Item '{InventoryItemId}' not found.", message.InventoryItemId);
            return;
        }

        var contentHtml = LowStockEmailBuilder.BuildHtml(item.Name, message.StockQuantity, message.ReorderLevel);
        var plainText = LowStockEmailBuilder.BuildPlainText(item.Name, message.StockQuantity, message.ReorderLevel);

        var fullEmailHtml = await templateRenderer.RenderAsync(
            title: LowStockEmailBuilder.Title,
            recipientName: "Workshop Administrator",
            messageHtml: contentHtml,
            ct: ct);

        await emailSender.SendEmailAsync(
            to: _emailSettings.AdminEmail,
            subject: $"⚠️ Low Stock Alert: {item.Name}",
            htmlBody: fullEmailHtml,
            textBody: plainText,
            ct: ct);
    }
}