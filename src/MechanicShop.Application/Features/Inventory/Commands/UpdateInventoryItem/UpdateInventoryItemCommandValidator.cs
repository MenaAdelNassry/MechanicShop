using FluentValidation;

namespace MechanicShop.Application.Features.Inventory.Commands.UpdateInventoryItem;

public sealed class UpdateInventoryItemCommandValidator : AbstractValidator<UpdateInventoryItemCommand>
{
    public UpdateInventoryItemCommandValidator()
    {
        RuleFor(x => x.InventoryItemId)
            .NotEmpty()
            .WithErrorCode("InventoryItem.IdRequired")
            .WithMessage("Inventory item ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("InventoryItem.NameRequired").WithMessage("Item name is required.")
            .MaximumLength(100).WithErrorCode("InventoryItem.NameTooLong").WithMessage("Item name must not exceed 100 characters.");

        RuleFor(x => x.Cost)
            .GreaterThan(0).WithErrorCode("InventoryItem.CostInvalid").WithMessage("Cost must be greater than 0.")
            .LessThanOrEqualTo(100_000).WithErrorCode("InventoryItem.CostTooHigh").WithMessage("Cost must not exceed 100,000.");

        RuleFor(x => x.ReorderLevel)
            .GreaterThanOrEqualTo(0).WithErrorCode("InventoryItem.ReorderLevelInvalid").WithMessage("Reorder level cannot be negative.");
    }
}