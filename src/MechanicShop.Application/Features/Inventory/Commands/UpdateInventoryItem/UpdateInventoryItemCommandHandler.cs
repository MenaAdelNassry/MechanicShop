using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Inventory.Commands.UpdateInventoryItem;

public class UpdateInventoryItemCommandHandler(IAppDbContext context) : IRequestHandler<UpdateInventoryItemCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(UpdateInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var item = await context.InventoryItems.FindAsync([request.InventoryItemId], cancellationToken);
        if (item is null) return ApplicationErrors.Inventory.NotFound;

        var normalizedName = request.Name.Trim();

        var nameExists = await context.InventoryItems
            .AnyAsync(x => x.Id != request.InventoryItemId && x.Name.ToLower() == normalizedName.ToLower(), cancellationToken);

        if (nameExists)
            return ApplicationErrors.Inventory.DuplicateName;

        var updateResult = item.UpdateDetails(request.Name, request.Cost, request.ReorderLevel);
        if (updateResult.IsError) return updateResult.Errors;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Updated;
    }
}
