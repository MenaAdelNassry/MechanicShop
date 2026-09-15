using FluentValidation;

namespace MechanicShop.Application.Features.Identity.Queries.RefreshTokens;

public sealed class RefreshTokenQueryValidator : AbstractValidator<RefreshTokenQuery>
{
    public RefreshTokenQueryValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithErrorCode("Identity.RefreshTokenRequired").WithMessage("Refresh token is required.");

        RuleFor(x => x.ExpiredAccessToken)
            .NotEmpty().WithErrorCode("Identity.AccessTokenRequired").WithMessage("Expired access token is required.");
    }
}