using FluentValidation;

using MechanicShop.Domain.Workorders.Enums;

namespace MechanicShop.Application.Features.WorkOrders.Commands.RelocateWorkOrder;

public sealed class RescheduleAppointmentCommandValidator : AbstractValidator<RelocateWorkOrderCommand>
{
    public RescheduleAppointmentCommandValidator()
    {
        RuleFor(x => x.WorkOrderId)
            .NotEmpty()
            .WithErrorCode("WorkOrderId_Required")
            .WithMessage("WorkOrderId is required.");

        RuleFor(x => x.NewStartAt)
            .GreaterThan(_ => DateTimeOffset.UtcNow)
            .WithMessage("New start time must be in the future.");

        RuleFor(x => x.NewSpot)
            .IsInEnum()
            .WithErrorCode("Spot_Invalid")
            .WithMessage($"Spot must be a valid Spot value: [{string.Join(", ", Enum.GetNames<Spot>())}].");
    }
}