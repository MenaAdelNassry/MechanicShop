using FluentValidation;

namespace MechanicShop.Application.Features.Scheduling.Queries.GetDailyScheduleQuery;

public sealed class GetDailyScheduleQueryValidator : AbstractValidator<GetDailyScheduleQuery>
{
    public GetDailyScheduleQueryValidator()
    {
        RuleFor(x => x.TimeZone)
            .NotNull()
            .WithMessage("TimeZone is required.");

        RuleFor(x => x.ScheduleDate)
            .NotEmpty()
            .WithMessage("Schedule date is required.");

        RuleFor(x => x.SlotDurationInMinutes)
            .Must(d => d is 5 or 10 or 15 or 30 or 60)
            .WithMessage("Slot duration must be 5, 10, 15, 30, or 60 minutes.");
    }
}