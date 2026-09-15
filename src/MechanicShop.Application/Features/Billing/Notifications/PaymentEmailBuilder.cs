namespace MechanicShop.Application.Features.Billing.Notifications;

public static class PaymentEmailBuilder
{
    public const string Subject = "💳 MechanicShop — Invoice Payment Link";
    public const string Title = "Settle Your Invoice Online";
    public const string ButtonText = "Pay Online Now";

    public static string BuildHtml(decimal amount) => $@"
        Your vehicle invoice is ready for settlement.
        <div style='background: #f8fafc; padding: 16px; margin: 16px 0; border-radius: 8px; border: 1px solid #e2e8f0;'>
            <strong>Amount Due:</strong> <span style='font-size: 18px; color: #0284c7; font-weight: bold;'>{amount:N2} EGP</span>
        </div>";

    public static string BuildPlainText(string customerName, decimal amount, string paymentUrl) =>
        $"Hello {customerName},\n\n" +
        $"Your invoice is ready for payment. Amount due: {amount:N2} EGP\n" +
        $"You can pay securely online using the following link:\n{paymentUrl}\n\n" +
        $"Thank you for choosing MechanicShop!";
}