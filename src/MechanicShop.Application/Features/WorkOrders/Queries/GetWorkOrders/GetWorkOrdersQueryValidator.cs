using FluentValidation;

using MechanicShop.Application.Common.Models;

namespace MechanicShop.Application.Features.WorkOrders.Queries.GetWorkOrders;

public sealed class GetWorkOrdersQueryValidator : AbstractValidator<GetWorkOrdersQuery>
{
    public GetWorkOrdersQueryValidator()
    {
        RuleFor(x => new PageRequest { Page = x.Page, PageSize = x.PageSize })
            .SetValidator(new PageRequestValidator());

        RuleFor(x => x.State)
            .IsInEnum()
            .When(x => x.State is not null)
            .WithMessage("Invalid work order state selected.");

        RuleFor(x => x.StartDateTo)
            .GreaterThanOrEqualTo(x => x.StartDateFrom!.Value)
            .When(x => x.StartDateFrom is not null && x.StartDateTo is not null)
            .WithMessage("Start date 'To' must be greater than or equal to 'From'.");

        RuleFor(x => x.EndDateTo)
            .GreaterThanOrEqualTo(x => x.EndDateFrom!.Value)
            .When(x => x.EndDateFrom is not null && x.EndDateTo is not null)
            .WithMessage("End date 'To' must be greater than or equal to 'From'.");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100)
            .When(x => x.SearchTerm is not null)
            .WithMessage("Search term cannot exceed 100 characters.");
    }
}