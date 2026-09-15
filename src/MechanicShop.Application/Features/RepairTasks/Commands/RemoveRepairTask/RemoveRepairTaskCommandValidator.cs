using FluentValidation;

namespace MechanicShop.Application.Features.RepairTasks.Commands.RemoveRepairTask;

public sealed class RemoveRepairTaskCommandValidator : AbstractValidator<RemoveRepairTaskCommand>
{
    public RemoveRepairTaskCommandValidator()
    {
        RuleFor(x => x.RepairTaskId)
            .NotEmpty().WithMessage("Repair task Id is required.");
    }
}