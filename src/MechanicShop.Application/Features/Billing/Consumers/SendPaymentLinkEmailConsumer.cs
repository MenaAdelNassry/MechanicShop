using MassTransit;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Billing.Events;
using MechanicShop.Application.Features.Billing.Notifications;

namespace MechanicShop.Application.Features.Billing.Consumers;

public sealed class SendPaymentLinkEmailConsumer(
    IEmailSender emailSender,
    IEmailTemplateRenderer templateRenderer) : IConsumer<PaymentLinkCreatedEvent>
{
    public async Task Consume(ConsumeContext<PaymentLinkCreatedEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        if (string.IsNullOrWhiteSpace(msg.CustomerEmail)) return;

        var messageHtml = PaymentEmailBuilder.BuildHtml(msg.Amount);
        var plainText = PaymentEmailBuilder.BuildPlainText(msg.CustomerName, msg.Amount, msg.PaymentUrl);

        var fullEmailHtml = await templateRenderer.RenderAsync(
            title: PaymentEmailBuilder.Title,
            recipientName: msg.CustomerName,
            messageHtml: messageHtml,
            buttonText: PaymentEmailBuilder.ButtonText,
            buttonUrl: msg.PaymentUrl,
            ct: ct);

        await emailSender.SendEmailAsync(
            to: msg.CustomerEmail,
            subject: PaymentEmailBuilder.Subject,
            htmlBody: fullEmailHtml,
            textBody: plainText,
            ct: ct);
    }
}