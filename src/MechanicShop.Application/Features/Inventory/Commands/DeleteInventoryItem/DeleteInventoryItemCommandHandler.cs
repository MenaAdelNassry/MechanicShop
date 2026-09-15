using System;

using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Inventory.Commands.DeleteInventoryItem;

public class DeleteInventoryItemCommandHandler(IAppDbContext context, TimeProvider timeProvider)
    : IRequestHandler<DeleteInventoryItemCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(DeleteInventoryItemCommand command, CancellationToken ct)
    {
        var item = await context.InventoryItems
            .FirstOrDefaultAsync(i => i.Id == command.Id, ct);

        if (item is null)
        {
            return ApplicationErrors.Inventory.NotFound;
        }

        var deleteResult = item.Delete(timeProvider);
        if (deleteResult.IsError)
        {
            return deleteResult.Errors;
        }

        await context.SaveChangesAsync(ct);

        return Result.Deleted;
    }
}