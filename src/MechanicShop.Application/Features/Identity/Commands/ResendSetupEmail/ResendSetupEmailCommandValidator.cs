using FluentValidation;

namespace MechanicShop.Application.Features.Identity.Commands.ResendSetupEmail;

public class ResendSetupEmailCommandValidator : AbstractValidator<ResendSetupEmailCommand>
{
    public ResendSetupEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email address must not exceed 256 characters.");
    }
}