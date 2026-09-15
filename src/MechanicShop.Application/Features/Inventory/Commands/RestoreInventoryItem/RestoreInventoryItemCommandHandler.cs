using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Inventory.Commands.RestoreInventoryItem;

public class RestoreInventoryItemCommandHandler(IAppDbContext context)
    : IRequestHandler<RestoreInventoryItemCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(RestoreInventoryItemCommand command, CancellationToken ct)
    {
        var item = await context.InventoryItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == command.Id, ct);

        if (item is null) return ApplicationErrors.Inventory.NotFound;

        var restoreResult = item.Restore();
        if (restoreResult.IsError) return restoreResult.Errors;

        await context.SaveChangesAsync(ct);
        return Result.Updated;
    }
}