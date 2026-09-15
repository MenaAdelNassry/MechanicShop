using MassTransit;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Billing.Events;
using MechanicShop.Application.Features.Billing.Notifications;

namespace MechanicShop.Application.Features.Billing.Consumers;

public sealed class SendPaymentLinkSmsConsumer(
    ISmsSender smsSender) : IConsumer<PaymentLinkCreatedEvent>
{
    public async Task Consume(ConsumeContext<PaymentLinkCreatedEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        if (string.IsNullOrWhiteSpace(msg.CustomerPhoneNumber)) return;

        var smsMessage = PaymentSmsBuilder.BuildSmsText(msg.Amount, msg.PaymentUrl);

        await smsSender.SendSmsAsync(
            phoneNumber: msg.CustomerPhoneNumber,
            message: smsMessage,
            ct: ct);
    }
}