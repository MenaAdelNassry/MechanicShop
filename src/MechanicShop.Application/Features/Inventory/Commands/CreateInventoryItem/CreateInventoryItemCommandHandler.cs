using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Inventory.Dtos;
using MechanicShop.Application.Features.Inventory.Mappers;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Inventory;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Inventory.Commands.CreateInventoryItem;

public class CreateInventoryItemCommandHandler(IAppDbContext context) : IRequestHandler<CreateInventoryItemCommand, Result<InventoryItemDto>>
{
    public async Task<Result<InventoryItemDto>> Handle(CreateInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var exists = await context.InventoryItems
            .AnyAsync(x => x.Name.ToLower() == request.Name.Trim().ToLower(), cancellationToken);

        if (exists)
            return Error.Conflict("InventoryItem.DuplicateName", $"An inventory item with name '{request.Name}' already exists.");

        var createResult = InventoryItem.Create(Guid.CreateVersion7(), request.Name, request.Cost, request.InitialStock, request.ReorderLevel);
        if (createResult.IsError) return createResult.Errors;

        var item = createResult.Value;
        context.InventoryItems.Add(item);

        await context.SaveChangesAsync(cancellationToken);

        return item.ToDto();
    }
}
