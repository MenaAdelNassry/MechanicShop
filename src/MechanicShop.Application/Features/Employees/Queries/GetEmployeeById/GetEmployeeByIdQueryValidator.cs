using FluentValidation;

namespace MechanicShop.Application.Features.Employees.Queries.GetEmployeeById;

public sealed class GetEmployeeByIdQueryValidator : AbstractValidator<GetEmployeeByIdQuery>
{
    public GetEmployeeByIdQueryValidator()
    {
        RuleFor(x => x.EmployeeId)
            .NotEmpty()
            .WithMessage("Employee Id is required.");
    }
}