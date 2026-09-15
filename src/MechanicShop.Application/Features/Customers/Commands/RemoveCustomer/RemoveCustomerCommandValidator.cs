using FluentValidation;

namespace MechanicShop.Application.Features.Customers.Commands.RemoveCustomer;

public class RemoveCustomerCommandValidator : AbstractValidator<RemoveCustomerCommand>
{
    public RemoveCustomerCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer Id is required.");
    }
}