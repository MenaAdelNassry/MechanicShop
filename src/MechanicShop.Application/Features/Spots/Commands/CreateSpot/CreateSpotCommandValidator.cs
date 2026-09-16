using FluentValidation;

using MechanicShop.Application.Features.Spots.Commands.CreateSpot;

public sealed class CreateSpotCommandValidator : AbstractValidator<CreateSpotCommand>
{
    public CreateSpotCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Service bay name is required.")
            .MaximumLength(50).WithMessage("Service bay name cannot exceed 50 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(250).When(x => !string.IsNullOrEmpty(x.Description));
    }
}