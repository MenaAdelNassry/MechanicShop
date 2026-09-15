using FluentValidation;

using MechanicShop.Application.Features.Inventory.Commands.AdjustInventoryItemStock;

public sealed class AdjustInventoryItemStockCommandValidator : AbstractValidator<AdjustInventoryItemStockCommand>
{
    public AdjustInventoryItemStockCommandValidator()
    {
        RuleFor(x => x.InventoryItemId)
            .NotEmpty().WithErrorCode("InventoryItem.IdRequired").WithMessage("Inventory item ID is required.");

        RuleFor(x => x.Quantity)
            .NotEqual(0).WithErrorCode("InventoryItem.QuantityZero").WithMessage("Adjustment quantity cannot be zero.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithErrorCode("InventoryItem.ReasonRequired").WithMessage("Reason for adjustment is required.")
            .MaximumLength(500).WithErrorCode("InventoryItem.ReasonTooLong").WithMessage("Reason must not exceed 500 characters.");
    }
}