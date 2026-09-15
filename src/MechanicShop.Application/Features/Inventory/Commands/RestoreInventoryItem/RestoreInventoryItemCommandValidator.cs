using FluentValidation;

namespace MechanicShop.Application.Features.Inventory.Commands.RestoreInventoryItem;

public sealed class RestoreInventoryItemCommandValidator : AbstractValidator<RestoreInventoryItemCommand>
{
    public RestoreInventoryItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("InventoryItem.IdRequired")
            .WithMessage("Inventory item ID is required.");
    }
}