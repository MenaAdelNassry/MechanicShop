using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Identity.Notifications;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Identity.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    IIdentityService identityService,
    IEmailSender emailSender,
    IEmailTemplateRenderer templateRenderer,
    ILogger<ForgotPasswordCommandHandler> logger)
    : IRequestHandler<ForgotPasswordCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var tokenResult = await identityService.GenerateForgotPasswordTokenAsync(request.Email, ct);

        if (tokenResult.IsError)
        {
            logger.LogWarning(
                "Password reset requested for non-existent or unconfirmed email: {Email}. Reason: {ErrorCode}",
                request.Email,
                tokenResult.TopError.Code);

            return Result.Success;
        }

        var messageHtml = ForgotPasswordEmailBuilder.BuildHtml(request.Email, tokenResult.Value);
        var plainText = ForgotPasswordEmailBuilder.BuildPlainText(request.Email, tokenResult.Value);

        var fullEmailHtml = await templateRenderer.RenderAsync(
            title: ForgotPasswordEmailBuilder.Title,
            recipientName: "User",
            messageHtml: messageHtml,
            ct: ct);

        await emailSender.SendEmailAsync(
            to: request.Email,
            subject: ForgotPasswordEmailBuilder.Subject,
            htmlBody: fullEmailHtml,
            textBody: plainText,
            ct: ct);

        logger.LogInformation("Password reset email sent to {Email}.", request.Email);

        return Result.Success;
    }
}