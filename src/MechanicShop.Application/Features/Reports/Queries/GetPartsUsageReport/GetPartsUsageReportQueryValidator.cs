using FluentValidation;

namespace MechanicShop.Application.Features.Reports.Queries.GetPartsUsageReport;

public sealed class GetPartsUsageReportQueryValidator : AbstractValidator<GetPartsUsageReportQuery>
{
    public GetPartsUsageReportQueryValidator()
    {
        RuleFor(x => x.FromDate)
            .NotEmpty().WithMessage("FromDate is required.");

        RuleFor(x => x.ToDate)
            .NotEmpty().WithMessage("ToDate is required.")
            .GreaterThanOrEqualTo(x => x.FromDate).WithMessage("ToDate must be greater than or equal to FromDate.");

        RuleFor(x => x.TimeZone)
            .NotNull().WithMessage("TimeZone is required.");
    }
}