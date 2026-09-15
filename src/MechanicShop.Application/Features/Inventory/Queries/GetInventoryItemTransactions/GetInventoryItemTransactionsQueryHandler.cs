using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Inventory.Dtos;
using MechanicShop.Application.Features.Inventory.Mappers;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Inventory.Queries.GetInventoryItemTransactions;

public sealed class GetInventoryItemTransactionsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetInventoryItemTransactionsQuery, Result<IReadOnlyList<InventoryTransactionDto>>>
{
    public async Task<Result<IReadOnlyList<InventoryTransactionDto>>> Handle(
        GetInventoryItemTransactionsQuery query,
        CancellationToken ct)
    {
        var itemExists = await context.InventoryItems
            .AnyAsync(i => i.Id == query.InventoryItemId, ct);

        if (!itemExists)
        {
            return ApplicationErrors.Inventory.NotFound;
        }

        var transactions = await context.InventoryTransactions
            .AsNoTracking()
            .Where(t => t.InventoryItemId == query.InventoryItemId)
            .OrderByDescending(t => t.OccurredAtUtc)
            .Select(t => t.ToDto())
            .ToListAsync(ct);

        return transactions;
    }
}