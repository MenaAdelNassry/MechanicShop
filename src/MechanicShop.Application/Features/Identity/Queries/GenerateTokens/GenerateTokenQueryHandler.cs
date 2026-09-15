using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Identity.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Identity.Queries.GenerateTokens;

public sealed class GenerateTokenQueryHandler(
    ILogger<GenerateTokenQueryHandler> logger,
    IIdentityService identityService,
    ITokenProvider tokenProvider)
    : IRequestHandler<GenerateTokenQuery, Result<TokenResponse>>
{
    public async Task<Result<TokenResponse>> Handle(GenerateTokenQuery query, CancellationToken ct)
    {
        var userResponse = await identityService.AuthenticateAsync(query.Email, query.Password);

        if (userResponse.IsError)
        {
            logger.LogWarning(
                "Authentication failed for email {Email}. Reason: {ErrorCode}",
                query.Email,
                userResponse.TopError.Code);

            return userResponse.Errors;
        }

        var generateTokenResult = await tokenProvider.GenerateJwtTokenAsync(userResponse.Value, ct);

        if (generateTokenResult.IsError)
        {
            logger.LogError(
                "Failed to generate JWT token for user {UserId}. Error: {ErrorDescription}",
                userResponse.Value.UserId,
                generateTokenResult.TopError.Description);

            return generateTokenResult.Errors;
        }

        return generateTokenResult.Value;
    }
}