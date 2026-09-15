namespace MechanicShop.Application.Common.Interfaces;

public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string htmlBody, string? textBody = null, CancellationToken ct = default);
}
