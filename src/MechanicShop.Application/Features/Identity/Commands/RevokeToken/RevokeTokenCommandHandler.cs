using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Identity.Commands.RevokeToken;

public sealed class RevokeTokenCommandHandler(
    IAppDbContext context,
    TimeProvider timeProvider,
    ILogger<RevokeTokenCommandHandler> logger)
    : IRequestHandler<RevokeTokenCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(RevokeTokenCommand request, CancellationToken ct)
    {
        var token = await context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, ct);

        if (token is null)
        {
            logger.LogWarning("Revoke failed: Refresh token not found.");
            return ApplicationErrors.Auth.RefreshTokenExpired;
        }

        var revokeResult = token.Revoke(timeProvider);
        if (revokeResult.IsError)
        {
            return revokeResult.Errors;
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Refresh token revoked successfully for User '{UserId}'.", token.UserId);

        return Result.Updated;
    }
}