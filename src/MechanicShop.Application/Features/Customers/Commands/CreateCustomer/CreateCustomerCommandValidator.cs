using FluentValidation;

namespace MechanicShop.Application.Features.Customers.Commands.CreateCustomer;

public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required").MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required").MaximumLength(50);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.");

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.");

        RuleFor(x => x.Vehicles)
            .NotNull().WithMessage("Vehicle list cannot be null.")
            .Must(p => p.Count > 0).WithMessage("At least one vehicle is required.");

        RuleForEach(x => x.Vehicles).SetValidator(new CreateVehicleCommandValidator());
    }
}