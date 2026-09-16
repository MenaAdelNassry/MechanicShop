using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Identity.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Identity.Queries.GetUserInfo;

public sealed class GetUserByIdQueryHandler(
    ILogger<GetUserByIdQueryHandler> logger,
    IIdentityService identityService,
    IAppDbContext context)
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

        var identityUser = getUserByIdResult.Value;
        var employee = await context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.IdentityUserId.ToString() == identityUser.UserId, ct);

        return new AppUserDto(
            identityUser.UserId,
            identityUser.Email,
            identityUser.Roles,
            employee?.Id,
            employee != null ? $"{employee.Name.FirstName} {employee.Name.LastName}" : identityUser.Email);
    }
}