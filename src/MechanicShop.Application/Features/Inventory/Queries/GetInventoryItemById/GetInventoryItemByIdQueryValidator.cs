using FluentValidation;

namespace MechanicShop.Application.Features.Inventory.Queries.GetInventoryItemById;

public sealed class GetInventoryItemByIdQueryValidator : AbstractValidator<GetInventoryItemByIdQuery>
{
    public GetInventoryItemByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("InventoryItem.IdRequired")
            .WithMessage("Inventory item ID is required.");
    }
}