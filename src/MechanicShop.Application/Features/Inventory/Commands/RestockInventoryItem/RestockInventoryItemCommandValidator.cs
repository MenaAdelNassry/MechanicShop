using FluentValidation;

namespace MechanicShop.Application.Features.Inventory.Commands.RestockInventoryItem;

public sealed class RestockInventoryItemCommandValidator : AbstractValidator<RestockInventoryItemCommand>
{
    public RestockInventoryItemCommandValidator()
    {
        RuleFor(x => x.InventoryItemId)
            .NotEmpty()
            .WithErrorCode("InventoryItem.IdRequired")
            .WithMessage("Inventory item ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithErrorCode("InventoryItem.InvalidQuantity")
            .WithMessage("Restock quantity must be greater than zero.")
            .LessThanOrEqualTo(100_000)
            .WithErrorCode("InventoryItem.QuantityTooLarge")
            .WithMessage("Restock quantity exceeds maximum limit.");
    }
}