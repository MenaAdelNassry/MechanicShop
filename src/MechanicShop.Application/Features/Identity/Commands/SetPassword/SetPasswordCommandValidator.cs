using FluentValidation;

namespace MechanicShop.Application.Features.Identity.Commands.SetPassword;

public sealed class SetPasswordCommandValidator : AbstractValidator<SetPasswordCommand>
{
    public SetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode("Identity.EmailRequired").WithMessage("Email address is required.")
            .EmailAddress().WithErrorCode("Identity.InvalidEmail").WithMessage("A valid email address is required.")
            .MaximumLength(256).WithErrorCode("Identity.EmailTooLong").WithMessage("Email address must not exceed 256 characters.");

        RuleFor(x => x.Token)
            .NotEmpty().WithErrorCode("Identity.TokenRequired").WithMessage("Token is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithErrorCode("Identity.PasswordRequired").WithMessage("Password is required.")
            .MinimumLength(8).WithErrorCode("Identity.PasswordTooShort").WithMessage("Password must be at least 8 characters long.")
            .Matches("(?=.*[a-z])").WithErrorCode("Identity.PasswordLowercase").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("(?=.*[A-Z])").WithErrorCode("Identity.PasswordUppercase").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("(?=.*\\d)").WithErrorCode("Identity.PasswordDigit").WithMessage("Password must contain at least one digit.")
            .Matches("(?=.*[^a-zA-Z0-9])").WithErrorCode("Identity.PasswordSpecial").WithMessage("Password must contain at least one special character.");
    }
}
