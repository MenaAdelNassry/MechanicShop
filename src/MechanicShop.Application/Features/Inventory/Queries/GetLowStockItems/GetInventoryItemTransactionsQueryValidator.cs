using FluentValidation;

namespace MechanicShop.Application.Features.Inventory.Queries.GetInventoryItemTransactions;

public sealed class GetInventoryItemTransactionsQueryValidator : AbstractValidator<GetInventoryItemTransactionsQuery>
{
    public GetInventoryItemTransactionsQueryValidator()
    {
        RuleFor(x => x.InventoryItemId)
            .NotEmpty()
            .WithErrorCode("InventoryItem.IdRequired")
            .WithMessage("Inventory item ID is required.");
    }
}