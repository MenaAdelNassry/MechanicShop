using MailKit.Net.Smtp;
using MailKit.Security;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Infrastructure.Settings;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MimeKit;

namespace MechanicShop.Infrastructure.Services;

public sealed class EmailSender(
    ILogger<EmailSender> logger,
    IOptions<EmailSettings> emailOptions) : IEmailSender
{
    private readonly EmailSettings _emailSettings = emailOptions.Value;

    public async Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        string? textBody = null,
        CancellationToken ct = default)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = WrapInBaseTemplate(subject, htmlBody),
            TextBody = textBody ?? htmlBody
        };
        email.Body = bodyBuilder.ToMessageBody();

        using var smtp = new SmtpClient();
        try
        {
            await smtp.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, SecureSocketOptions.StartTls, ct);
            await smtp.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password, ct);
            await smtp.SendAsync(email, ct);

            logger.LogInformation("[Email] Sent successfully to {Email} | Subject: '{Subject}'", to, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Email] Failed sending to {Email} | Subject: '{Subject}'", to, subject);
            throw;
        }
        finally
        {
            await smtp.DisconnectAsync(true, ct);
        }
    }

    private static string WrapInBaseTemplate(string title, string content)
    {
        return $@"
        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;'>
            <div style='background: #0284c7; color: white; padding: 16px 24px;'>
                <h2 style='margin:0; font-size: 20px;'>{title}</h2>
            </div>
            <div style='padding: 24px; color: #334155; line-height: 1.6;'>
                {content}
            </div>
            <div style='background: #f8fafc; padding: 12px 24px; text-align: center; font-size: 12px; color: #94a3b8; border-top: 1px solid #e2e8f0;'>
                MechanicShop Management System
            </div>
        </div>";
    }
}