using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Application.Features.RepairTasks.Mappers;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.RepairTasks.Queries.GetDeletedRepairTasks;

public sealed class GetDeletedRepairTasksQueryHandler(IAppDbContext context)
    : IRequestHandler<GetDeletedRepairTasksQuery, Result<IReadOnlyList<RepairTaskDto>>>
{
    public async Task<Result<IReadOnlyList<RepairTaskDto>>> Handle(GetDeletedRepairTasksQuery request, CancellationToken ct)
    {
        var deletedTasks = await context.RepairTasks
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(t => t.Parts)
            .Where(t => t.IsDeleted)
            .ToListAsync(ct);

        if (deletedTasks.Count == 0)
        {
            return Array.Empty<RepairTaskDto>();
        }

        var itemIds = deletedTasks
            .SelectMany(t => t.Parts)
            .Select(p => p.InventoryItemId)
            .Distinct()
            .ToList();

        var inventoryItemsMap = await context.InventoryItems
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        return deletedTasks.ToDtos(inventoryItemsMap);
    }
}