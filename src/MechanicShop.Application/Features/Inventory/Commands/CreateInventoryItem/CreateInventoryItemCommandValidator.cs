using FluentValidation;

using MechanicShop.Application.Features.Inventory.Commands.CreateInventoryItem;

public sealed class CreateInventoryItemCommandValidator : AbstractValidator<CreateInventoryItemCommand>
{
    public CreateInventoryItemCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("InventoryItem.NameRequired").WithMessage("Item name is required.")
            .MaximumLength(100).WithErrorCode("InventoryItem.NameTooLong").WithMessage("Item name must not exceed 100 characters.");

        RuleFor(x => x.Cost)
            .GreaterThan(0).WithErrorCode("InventoryItem.CostInvalid").WithMessage("Cost must be greater than 0.")
            .LessThanOrEqualTo(100_000).WithErrorCode("InventoryItem.CostTooHigh").WithMessage("Cost must not exceed 100,000.");

        RuleFor(x => x.InitialStock)
            .GreaterThanOrEqualTo(0).WithErrorCode("InventoryItem.InitialStockInvalid").WithMessage("Initial stock cannot be negative.");

        RuleFor(x => x.ReorderLevel)
            .GreaterThanOrEqualTo(0).WithErrorCode("InventoryItem.ReorderLevelInvalid").WithMessage("Reorder level cannot be negative.");
    }
}