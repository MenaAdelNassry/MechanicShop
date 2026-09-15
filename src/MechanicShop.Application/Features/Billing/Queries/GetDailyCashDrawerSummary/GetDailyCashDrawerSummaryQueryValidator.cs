using FluentValidation;

namespace MechanicShop.Application.Features.Billing.Queries.GetDailyCashDrawerSummary;

public sealed class GetDailyCashDrawerSummaryQueryValidator : AbstractValidator<GetDailyCashDrawerSummaryQuery>
{
    public GetDailyCashDrawerSummaryQueryValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required.");

        RuleFor(x => x.TimeZoneId)
            .Must(BeAValidTimeZone)
            .When(x => !string.IsNullOrWhiteSpace(x.TimeZoneId))
            .WithMessage("Invalid TimeZone identifier.");
    }

    private bool BeAValidTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId)) return true;

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
    }
}