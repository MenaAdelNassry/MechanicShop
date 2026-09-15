namespace MechanicShop.Application.Features.Identity.Notifications;

public static class EmployeeWelcomeEmailBuilder
{
    public const string Subject = "🎉 Welcome to MechanicShop — Set Up Your Account";
    public const string Title = "Welcome to the Team!";

    public static string BuildHtml(string email, string token) => $@"
        Welcome to <strong>MechanicShop</strong>! Your employee account is ready.<br/><br/>
        Please complete your account activation by setting up your password.<br/><br/>
        <div style='background: #f8fafc; padding: 16px; border-radius: 8px; border: 1px solid #cbd5e1; font-family: monospace;'>
            <strong>Email:</strong> {email}<br/>
            <strong>Reset Token:</strong> <span style='word-break: break-all; color: #0284c7;'>{token}</span>
        </div>
        <br/>
        Use this token to set your password and confirm your email.";

    public static string BuildPlainText(string email, string token) =>
        $"Welcome to MechanicShop!\n\n" +
        $"Your account has been created. Use the following details to set your password:\n" +
        $"Email: {email}\n" +
        $"Token: {token}\n\n" +
        $"Submit these details to complete your setup.";
}