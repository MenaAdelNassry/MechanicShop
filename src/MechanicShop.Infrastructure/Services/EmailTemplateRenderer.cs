using MechanicShop.Application.Common.Interfaces;

using Microsoft.Extensions.Logging;

namespace MechanicShop.Infrastructure.Services;

public sealed class EmailTemplateRenderer(ILogger<EmailTemplateRenderer> logger) : IEmailTemplateRenderer
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    private static string? _cachedTemplate;

    public async Task<string> RenderAsync(
        string title,
        string recipientName,
        string messageHtml,
        string? buttonText = null,
        string? buttonUrl = null,
        CancellationToken ct = default)
    {
        var template = await GetTemplateAsync(ct);

        var actionHtml = string.Empty;
        if (!string.IsNullOrWhiteSpace(buttonText) && !string.IsNullOrWhiteSpace(buttonUrl))
        {
            actionHtml = $@"
                <div style='text-align: center; margin: 28px 0;'>
                    <a href='{buttonUrl}' style='background-color: #0284c7; color: #ffffff; padding: 12px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block; box-shadow: 0 2px 4px rgba(2, 132, 199, 0.2);'>
                        {buttonText}
                    </a>
                </div>
                <p style='text-align: center; font-size: 13px; color: #64748b;'>
                    Or copy this link: <a href='{buttonUrl}' style='color: #0284c7;'>{buttonUrl}</a>
                </p>";
        }

        return template
            .Replace("{{Title}}", title)
            .Replace("{{RecipientName}}", recipientName)
            .Replace("{{Message}}", messageHtml)
            .Replace("{{ActionSection}}", actionHtml);
    }

    private async Task<string> GetTemplateAsync(CancellationToken ct)
    {
        if (_cachedTemplate is not null)
        {
            return _cachedTemplate;
        }

        await Semaphore.WaitAsync(ct);
        try
        {
            if (_cachedTemplate is not null)
            {
                return _cachedTemplate;
            }

            var path = Path.Combine(AppContext.BaseDirectory, "Templates", "EmailTemplate.html");
            if (!File.Exists(path))
            {
                logger.LogError("Email template file not found at: {Path}", path);
                return "<h2>{{Title}}</h2><p>Hello {{RecipientName}},</p><div>{{Message}}</div>{{ActionSection}}";
            }

            _cachedTemplate = await File.ReadAllTextAsync(path, ct);
            return _cachedTemplate;
        }
        finally
        {
            Semaphore.Release();
        }
    }
}