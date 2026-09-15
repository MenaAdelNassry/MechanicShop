using System.Security.Claims;

using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Identity.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Identity.Queries.RefreshTokens;

public class RefreshTokenQueryHandler(ILogger<RefreshTokenQueryHandler> logger, IIdentityService identityService, IAppDbContext context, ITokenProvider tokenProvider)
    : IRequestHandler<RefreshTokenQuery, Result<TokenResponse>>
{
    public async Task<Result<TokenResponse>> Handle(RefreshTokenQuery request, CancellationToken ct)
    {
        var principal = tokenProvider.GetPrincipalFromExpiredToken(request.ExpiredAccessToken);

        if (principal is null)
        {
            logger.LogError("Expired access token is not valid");

            return ApplicationErrors.Auth.ExpiredAccessTokenInvalid;
        }

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId is null)
        {
            logger.LogError("Invalid userId claim");

            return ApplicationErrors.Auth.UserIdClaimInvalid;
        }

        var getUserResult = await identityService.GetUserByIdAsync(userId);

        if (getUserResult.IsError)
        {
            logger.LogError("Get user by id error occurred: {ErrorDescription}", getUserResult.TopError.Description);
            return getUserResult.Errors;
        }

        var refreshToken = await context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, ct);

        if (refreshToken is null)
        {
            logger.LogWarning("Security Alert: Refresh token {Token} does not exist in DB", request.RefreshToken);
            return ApplicationErrors.Auth.RefreshTokenExpired;
        }

        if (refreshToken.UserId != userId)
        {
            logger.LogCritical(
                "Security Violation: User {UserId} tried to use a refresh token belonging to User {OwnerId}",
                userId, refreshToken.UserId);
            return ApplicationErrors.Auth.RefreshTokenExpired;
        }

        if (refreshToken.IsRevoked || refreshToken.ExpiresOnUtc < DateTime.UtcNow)
        {
            logger.LogWarning("Refresh token {Token} is either revoked or expired.", request.RefreshToken);
            return ApplicationErrors.Auth.RefreshTokenExpired;
        }

        var generateTokenResult = await tokenProvider.GenerateJwtTokenAsync(getUserResult.Value, ct);

        if (generateTokenResult.IsError)
        {
            logger.LogError("Generate token error occurred: {ErrorDescription}", generateTokenResult.TopError.Description);

            return generateTokenResult.Errors;
        }

        // (Token Rotation)
        var revokedRefreshToken = refreshToken.Revoke(TimeProvider.System);
        if(revokedRefreshToken.IsError) return revokedRefreshToken.Errors;

        await context.SaveChangesAsync(ct);

        return generateTokenResult.Value;
    }
}