using FluentValidation;

namespace MechanicShop.Application.Features.Billing.Queries.GetInvoices;

public sealed class GetInvoicesQueryValidator : AbstractValidator<GetInvoicesQuery>
{
    public GetInvoicesQueryValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue)
            .WithMessage("Invalid invoice status provided.");

        When(x => x.FromDateUtc.HasValue && x.ToDateUtc.HasValue, () =>
        {
            RuleFor(x => x.FromDateUtc!.Value)
                .LessThanOrEqualTo(x => x.ToDateUtc!.Value)
                .WithMessage("FromDateUtc must be earlier than or equal to ToDateUtc.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.SearchTerm), () =>
        {
            RuleFor(x => x.SearchTerm)
                .MaximumLength(100)
                .WithMessage("Search term cannot exceed 100 characters.");
        });
    }
}