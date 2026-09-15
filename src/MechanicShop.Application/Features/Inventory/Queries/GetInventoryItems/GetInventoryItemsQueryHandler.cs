using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Inventory.Dtos;
using MechanicShop.Application.Features.Inventory.Mappers;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Inventory.Queries.GetInventoryItems;

public sealed class GetInventoryItemsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetInventoryItemsQuery, Result<IReadOnlyList<InventoryItemDto>>>
{
    public async Task<Result<IReadOnlyList<InventoryItemDto>>> Handle(GetInventoryItemsQuery request, CancellationToken ct)
    {
        var items = await context.InventoryItems
            .AsNoTracking()
            .ToListAsync(ct);

        return items.Select(i => i.ToDto()).ToList();
    }
}