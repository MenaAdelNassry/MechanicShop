using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Inventory.Dtos;
using MechanicShop.Application.Features.Inventory.Mappers;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Inventory.Queries.GetArchivedInventoryItems;

public class GetArchivedInventoryItemsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetArchivedInventoryItemsQuery, Result<List<InventoryItemDto>>>
{
    public async Task<Result<List<InventoryItemDto>>> Handle(GetArchivedInventoryItemsQuery request, CancellationToken ct)
    {
        var archivedItems = await context.InventoryItems
            .IgnoreQueryFilters()
            .Where(i => i.IsDeleted)
            .AsNoTracking()
            .ToListAsync(ct);

        return archivedItems.Select(i => i.ToDto()).ToList();
    }
}