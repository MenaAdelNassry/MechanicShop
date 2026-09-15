using FluentValidation;

namespace MechanicShop.Application.Features.Identity.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("Identity.EmailRequired").WithMessage("Email address is required.")
            .EmailAddress().WithErrorCode("Identity.InvalidEmail").WithMessage("A valid email address is required.")
            .MaximumLength(256).WithErrorCode("Identity.EmailTooLong").WithMessage("Email address must not exceed 256 characters.");
    }
}
