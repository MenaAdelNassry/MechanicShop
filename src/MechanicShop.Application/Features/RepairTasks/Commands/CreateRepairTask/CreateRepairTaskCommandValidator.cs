using FluentValidation;

namespace MechanicShop.Application.Features.RepairTasks.Commands.CreateRepairTask;

public sealed class CreateRepairTaskCommandValidator : AbstractValidator<CreateRepairTaskCommand>
{
    public CreateRepairTaskCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100);

        RuleFor(x => x.LaborCost)
            .GreaterThan(0).WithMessage("Labor cost must be greater than 0.");

        RuleFor(x => x.EstimatedDurationInMins)
            .NotNull().WithMessage("Estimated duration is required.")
            .IsInEnum();

        RuleFor(x => x.Parts)
            .NotNull().WithMessage("Parts list cannot be null.")
            .Must(p => p != null && p.Count > 0).WithMessage("At least one part is required.")
            .Must(parts => parts.Select(p => p.InventoryItemId).Distinct().Count() == parts.Count)
            .WithMessage("Duplicate inventory items are not allowed in the parts list.");

        RuleForEach(x => x.Parts).SetValidator(new CreateRepairTaskPartCommandValidator());
    }
}