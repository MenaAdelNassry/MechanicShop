using FluentValidation;

namespace MechanicShop.Application.Features.WorkOrders.Commands.UpdateWorkOrderRepairTasks;

public sealed class UpdateWorkOrderRepairTasksCommandValidator : AbstractValidator<UpdateWorkOrderRepairTasksCommand>
{
    public UpdateWorkOrderRepairTasksCommandValidator()
    {
        RuleFor(x => x.WorkOrderId)
            .NotEmpty()
            .WithErrorCode("WorkOrderId_Required")
            .WithMessage("WorkOrderId is required.");

        RuleFor(x => x.RepairTaskIds)
            .NotEmpty()
            .WithErrorCode("RepairTasks_Required")
            .WithMessage("At least one repair task must be provided.")
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("RepairTaskIds contains an invalid empty GUID.");
    }
}