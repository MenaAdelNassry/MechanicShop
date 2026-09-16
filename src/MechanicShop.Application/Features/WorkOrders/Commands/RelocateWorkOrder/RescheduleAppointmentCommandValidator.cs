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

        RuleFor(x => x.NewSpotId)
            .NotEmpty()
            .WithErrorCode("NewSpotId_Required")
            .WithMessage("NewSpotId is required.");

        RuleFor(x => x.NewStartAt)
            .GreaterThan(_ => DateTimeOffset.UtcNow)
            .WithMessage("New start time must be in the future.");
    }
}