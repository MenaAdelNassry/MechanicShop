using FluentValidation;

namespace MechanicShop.Application.Features.Inventory.Commands.DeleteInventoryItem;

public sealed class DeleteInventoryItemCommandValidator : AbstractValidator<DeleteInventoryItemCommand>
{
    public DeleteInventoryItemCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("InventoryItem.IdRequired")
            .WithMessage("Inventory item ID is required.");
    }
}