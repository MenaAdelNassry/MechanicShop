using FluentValidation;

namespace MechanicShop.Application.Features.Customers.Queries.GetCustomerHistory;

public sealed class GetCustomerHistoryQueryValidator : AbstractValidator<GetCustomerHistoryQuery>
{
    public GetCustomerHistoryQueryValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("Customer Id is required.");
    }
}