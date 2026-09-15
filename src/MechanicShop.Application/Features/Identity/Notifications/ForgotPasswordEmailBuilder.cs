namespace MechanicShop.Application.Features.Identity.Notifications;

public static class ForgotPasswordEmailBuilder
{
    public const string Subject = "🔒 Reset Your Password — MechanicShop";
    public const string Title = "Password Reset Request";

    public static string BuildHtml(string email, string token) =>
        $"""
        <p>We received a request to reset your password for account: <strong>{email}</strong>.</p>
        <p>Use the following token to complete your password reset:</p>
        <p style="word-break: break-all; font-family: monospace; background-color: #f4f4f4; padding: 10px; border-radius: 4px;">{token}</p>
        <p>If you did not request this, you can safely ignore this email. Your password will remain unchanged.</p>
        """;

    public static string BuildPlainText(string email, string token) =>
        $"""
        Password Reset Request

        We received a request to reset your password for: {email}
        Reset Token: {token}

        If you did not request this, please ignore this email.
        """;
}