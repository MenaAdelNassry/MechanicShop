using FluentValidation;

namespace MechanicShop.Application.Features.RepairTasks.Commands.UpdateRepairTask;

public sealed class UpdateRepairTaskPartCommandValidator : AbstractValidator<UpdateRepairTaskPartCommand>
{
    public UpdateRepairTaskPartCommandValidator()
    {
        RuleFor(x => x.InventoryItemId)
            .NotEmpty().WithMessage("InventoryItem ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be at least 1.");
    }
}