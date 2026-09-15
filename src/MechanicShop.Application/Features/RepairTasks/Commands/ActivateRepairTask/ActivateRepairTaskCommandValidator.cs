using FluentValidation;

namespace MechanicShop.Application.Features.RepairTasks.Commands.ActivateRepairTask;

public sealed class ActivateRepairTaskCommandValidator : AbstractValidator<ActivateRepairTaskCommand>
{
    public ActivateRepairTaskCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("RepairTask.IdRequired")
            .WithMessage("Repair task ID is required.");
    }
}