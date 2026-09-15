namespace MechanicShop.Application.Common.Interfaces;

public interface IEmailTemplateRenderer
{
    Task<string> RenderAsync(
        string title,
        string recipientName,
        string messageHtml,
        string? buttonText = null,
        string? buttonUrl = null,
        CancellationToken ct = default);
}