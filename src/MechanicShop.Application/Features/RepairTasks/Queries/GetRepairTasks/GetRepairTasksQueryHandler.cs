using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Application.Features.RepairTasks.Mappers;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.RepairTasks.Queries.GetRepairTasks;

public sealed class GetRepairTasksQueryHandler(IAppDbContext context)
    : IRequestHandler<GetRepairTasksQuery, Result<List<RepairTaskDto>>>
{
    public async Task<Result<List<RepairTaskDto>>> Handle(GetRepairTasksQuery query, CancellationToken ct)
    {
        var repairTasks = await context.RepairTasks
            .Include(rt => rt.Parts)
            .AsNoTracking()
            .ToListAsync(ct);

        var requestedInventoryIds = repairTasks
            .SelectMany(rt => rt.Parts)
            .Select(p => p.InventoryItemId)
            .Distinct()
            .ToList();

        var inventoryItemsMap = await context.InventoryItems
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(i => requestedInventoryIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        return repairTasks.ToDtos(inventoryItemsMap);
    }
}