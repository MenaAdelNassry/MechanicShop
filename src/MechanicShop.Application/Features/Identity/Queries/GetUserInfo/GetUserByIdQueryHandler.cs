using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Identity.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Identity.Queries.GetUserInfo;

public sealed class GetUserByIdQueryHandler(
    ILogger<GetUserByIdQueryHandler> logger,
    IIdentityService identityService)
    : IRequestHandler<GetUserByIdQuery, Result<AppUserDto>>
{
    public async Task<Result<AppUserDto>> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            return Error.Validation("Identity.UserIdRequired", "User Id is required.");

        var getUserByIdResult = await identityService.GetUserByIdAsync(request.UserId);

        if (getUserByIdResult.IsError)
        {
            logger.LogWarning(
                "Failed to retrieve user info for Id {UserId}. Reason: {ErrorDetails}",
                request.UserId,
                getUserByIdResult.TopError.Description);

            return getUserByIdResult.Errors;
        }

        return getUserByIdResult.Value;
    }
}