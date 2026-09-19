using Asp.Versioning;

using MechanicShop.Api.Extensions;
using MechanicShop.Api.Requests.Identity;
using MechanicShop.Application.Features.Identity.Commands.ForgotPassword;
using MechanicShop.Application.Features.Identity.Commands.ResendSetupEmail;
using MechanicShop.Application.Features.Identity.Commands.RevokeToken;
using MechanicShop.Application.Features.Identity.Commands.SetPassword;
using MechanicShop.Application.Features.Identity.Dtos;
using MechanicShop.Application.Features.Identity.Queries.GenerateTokens;
using MechanicShop.Application.Features.Identity.Queries.GetUserInfo;
using MechanicShop.Application.Features.Identity.Queries.RefreshTokens;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicShop.Api.Controllers;

[Route("identity")]
[ApiVersionNeutral]
public sealed class IdentityController(ISender sender) : ApiController
{
    [HttpPost("token/generate")]
    [EndpointSummary("Generates an access and refresh token for a valid user.")]
    [EndpointDescription("Authenticates a user using provided credentials and returns a JWT token pair.")]
    [EndpointName("GenerateToken")]
    public async Task<ActionResult<TokenResponse>> GenerateToken([FromBody] GenerateTokenQuery request, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("token/refresh-token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [EndpointSummary("Refreshes access token using a valid refresh token.")]
    [EndpointDescription("Exchanges an expired access token and a valid refresh token for a new token pair.")]
    [EndpointName("RefreshToken")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenQuery request, CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("current-user/claims")]
    [Authorize]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Gets the current authenticated user's info.")]
    [EndpointDescription("Returns user information for the currently authenticated user based on the access token.")]
    [EndpointName("GetCurrentUserClaims")]
    public async Task<ActionResult<AppUserDto>> GetCurrentUserInfo(CancellationToken ct)
    {
        var userId = User.GetUserId().ToString();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await sender.Send(new GetUserByIdQuery(userId), ct);

        return result.Match(Ok, Problem);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EndpointSummary("Sends a password reset link to the user's email.")]
    [EndpointName("ForgotPassword")]
    public async Task<IActionResult> ForgotPassword([FromBody] Requests.Identity.ForgotPasswordRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ForgotPasswordCommand(request.Email), ct);

        return Ok(new { Message = "If the email is registered, a password reset link has been sent." });
    }

    [HttpPost("set-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Activates a new employee account and sets their initial password.")]
    [EndpointDescription("Validates the initial setup token provided via onboarding email, updates the employee's password, and marks their email as confirmed.")]
    [EndpointName("SetPassword")]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SetPasswordCommand(request.Email, request.Token, request.NewPassword), ct);

        return result.Match(
            _ => Ok(new { Message = "Account activated and password set successfully." }),
            Problem);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Resets the password for a user using a reset token.")]
    [EndpointDescription("Validates the password reset token sent via email and replaces the user's current password with the new password.")]
    [EndpointName("ResetPassword")]
    public async Task<IActionResult> ResetPassword([FromBody] SetPasswordRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new SetPasswordCommand(request.Email, request.Token, request.NewPassword), ct);

        return result.Match(
            _ => Ok(new { Message = "Password reset successfully." }),
            Problem);
    }

    [HttpPost("resend-setup-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Resends the initial account setup email with a new token.")]
    [EndpointDescription("Generates a new password reset token and resends the onboarding email for employees who have not yet confirmed their accounts or whose tokens expired.")]
    [EndpointName("ResendSetupEmail")]
    public async Task<IActionResult> ResendSetupEmail([FromBody] ResendSetupEmailRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ResendSetupEmailCommand(request.Email), ct);

        return result.Match(
            _ => Ok(new { Message = "Setup email has been resent successfully." }),
            Problem);
    }

    [HttpPost("token/revoke")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Revokes a refresh token (Logout).")]
    [EndpointDescription("Revokes the specified refresh token, preventing it from being used to generate new access tokens.")]
    [EndpointName("RevokeToken")]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RevokeTokenCommand(request.RefreshToken), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}