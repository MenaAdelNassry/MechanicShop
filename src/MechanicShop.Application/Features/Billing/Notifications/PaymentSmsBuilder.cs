namespace MechanicShop.Application.Features.Billing.Notifications;

public static class PaymentSmsBuilder
{
    public static string BuildSmsText(decimal amount, string paymentUrl) =>
    $"MechanicShop: Your invoice of {amount:N2} EGP is ready for payment. Pay online: {paymentUrl}";
}