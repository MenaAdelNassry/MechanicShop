using FluentValidation;

namespace MechanicShop.Application.Features.Identity.Queries.GenerateTokens;

public sealed class GenerateTokenQueryValidator : AbstractValidator<GenerateTokenQuery>
{
    public GenerateTokenQueryValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("Identity.EmailRequired").WithMessage("Email is required.")
            .EmailAddress().WithErrorCode("Identity.InvalidEmail").WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode("Identity.PasswordRequired").WithMessage("Password is required.");
    }
}