using FluentValidation;

namespace MechanicShop.Application.Features.Reports.Queries.GetRevenueSummary;

public sealed class GetRevenueSummaryQueryValidator : AbstractValidator<GetRevenueSummaryQuery>
{
    public GetRevenueSummaryQueryValidator()
    {
        RuleFor(x => x.FromDate)
            .NotEmpty().WithMessage("FromDate is required.");

        RuleFor(x => x.ToDate)
            .NotEmpty().WithMessage("ToDate is required.")
            .GreaterThanOrEqualTo(x => x.FromDate).WithMessage("ToDate must be greater than or equal to FromDate.")
            .Must((query, toDate) => toDate.DayNumber - query.FromDate.DayNumber <= 366)
            .WithMessage("Date range cannot exceed one year.");

        RuleFor(x => x.TimeZone)
            .NotNull().WithMessage("TimeZone is required.");
    }
}