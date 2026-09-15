using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Inventory.Dtos;
using MechanicShop.Application.Features.Inventory.Mappers;
using MechanicShop.Application.Features.Inventory.Queries.GetInventoryItemById;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Inventory.Queries.GetInventoryItemById;

public class GetInventoryItemByIdQueryHandler(IAppDbContext context)
    : IRequestHandler<GetInventoryItemByIdQuery, Result<InventoryItemDto>>
{
    public async Task<Result<InventoryItemDto>> Handle(GetInventoryItemByIdQuery request, CancellationToken ct)
    {
        var item = await context.InventoryItems
            .Include(i => i.Transactions.OrderByDescending(t => t.OccurredAtUtc))
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct);

        if (item is null) return ApplicationErrors.Inventory.NotFound;

        return item.ToDto();
    }
}