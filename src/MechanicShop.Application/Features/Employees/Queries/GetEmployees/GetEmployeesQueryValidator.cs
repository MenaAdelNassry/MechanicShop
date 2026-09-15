using FluentValidation;

using MechanicShop.Domain.Identity;

namespace MechanicShop.Application.Features.Employees.Queries.GetEmployees;

public sealed class GetEmployeesQueryValidator : AbstractValidator<GetEmployeesQuery>
{
    public GetEmployeesQueryValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.Role), () =>
        {
            RuleFor(x => x.Role)
                .Must(role => Enum.TryParse<Role>(role, true, out _))
                .WithMessage("Invalid role specified.");
        });
    }
}