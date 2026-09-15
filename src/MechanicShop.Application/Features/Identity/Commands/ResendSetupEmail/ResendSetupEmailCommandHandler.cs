using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Identity.Notifications;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Identity.Commands.ResendSetupEmail;

public sealed class ResendSetupEmailCommandHandler(
    IIdentityService identityService,
    IEmailSender emailSender,
    IEmailTemplateRenderer templateRenderer,
    ILogger<ResendSetupEmailCommandHandler> logger)
    : IRequestHandler<ResendSetupEmailCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ResendSetupEmailCommand request, CancellationToken ct)
    {
        var tokenResult = await identityService.GeneratePasswordResetTokenByEmailAsync(request.Email, ct);

        if (tokenResult.IsError)
        {
            logger.LogWarning(
                "Failed to generate setup token for {Email}. Error: {ErrorCode}",
                request.Email,
                tokenResult.TopError.Code);

            return tokenResult.Errors;
        }

        var messageHtml = EmployeeWelcomeEmailBuilder.BuildHtml(request.Email, tokenResult.Value);
        var plainText = EmployeeWelcomeEmailBuilder.BuildPlainText(request.Email, tokenResult.Value);

        var fullEmailHtml = await templateRenderer.RenderAsync(
            title: EmployeeWelcomeEmailBuilder.Title,
            recipientName: "Team Member",
            messageHtml: messageHtml,
            ct: ct);

        await emailSender.SendEmailAsync(
            to: request.Email,
            subject: EmployeeWelcomeEmailBuilder.Subject,
            htmlBody: fullEmailHtml,
            textBody: plainText,
            ct: ct);

        logger.LogInformation("Resent invitation/setup email to {Email}.", request.Email);

        return Result.Success;
    }
}