using FluentValidation;

namespace MechanicShop.Application.Features.Customers.Commands.CreateCustomer;

public sealed class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleCommandValidator()
    {
        RuleFor(x => x.Make)
            .NotEmpty().MaximumLength(50);

        RuleFor(x => x.Model)
            .NotEmpty().MaximumLength(50);

        RuleFor(x => x.LicensePlate)
            .NotEmpty().MaximumLength(10);
    }
}
