using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Inventory.Commands.AdjustInventoryItemStock;

public class AdjustInventoryItemStockCommandHandler(IAppDbContext context) : IRequestHandler<AdjustInventoryItemStockCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(AdjustInventoryItemStockCommand request, CancellationToken cancellationToken)
    {
        var item = await context.InventoryItems.FindAsync([request.InventoryItemId], cancellationToken);
        if (item is null) return Error.NotFound("InventoryItem.NotFound", "Inventory item not found.");

        var result = item.AdjustStock(request.Quantity, request.Reason, request.PerformedBy);

        if (result.IsError)
            return result.Errors;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
