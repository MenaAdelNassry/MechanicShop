using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Inventory.Dtos;
using MechanicShop.Application.Features.Inventory.Mappers;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Inventory.Queries.GetLowStockItems;

public class GetLowStockItemsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetLowStockItemsQuery, Result<List<InventoryItemDto>>>
{
    public async Task<Result<List<InventoryItemDto>>> Handle(GetLowStockItemsQuery request, CancellationToken ct)
    {
        var items = await context.InventoryItems
            .AsNoTracking()
            .Where(i => i.StockQuantity <= i.ReorderLevel)
            .ToListAsync(ct);

        return items.Select(i => i.ToDto()).ToList();
    }
}