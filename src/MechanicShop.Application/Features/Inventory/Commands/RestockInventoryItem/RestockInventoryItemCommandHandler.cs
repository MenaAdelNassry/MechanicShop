using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Inventory.Commands.RestockInventoryItem;

public class RestockInventoryItemCommandHandler(IAppDbContext context) : IRequestHandler<RestockInventoryItemCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(RestockInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var item = await context.InventoryItems.FindAsync([request.InventoryItemId], cancellationToken);
        if (item is null) return Error.NotFound("InventoryItem.NotFound", "Inventory item not found.");

        var result = item.Restock(request.Quantity);

        if (result.IsError)
            return result.Errors;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
