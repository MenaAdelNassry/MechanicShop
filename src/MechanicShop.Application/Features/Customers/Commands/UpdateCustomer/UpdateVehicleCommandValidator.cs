using FluentValidation;

namespace MechanicShop.Application.Features.Customers.Commands.UpdateCustomer;

public sealed class UpdateVehicleCommandValidator : AbstractValidator<UpdateVehicleCommand>
{
    public UpdateVehicleCommandValidator()
    {
        RuleFor(x => x.Make)
            .NotEmpty().MaximumLength(50);

        RuleFor(x => x.Model)
            .NotEmpty().MaximumLength(50);

        RuleFor(x => x.LicensePlate)
            .NotEmpty().MaximumLength(10);
    }
}