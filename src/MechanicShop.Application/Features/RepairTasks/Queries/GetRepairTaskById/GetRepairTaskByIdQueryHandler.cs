using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Application.Features.RepairTasks.Mappers;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.RepairTasks.Queries.GetRepairTaskById;

public sealed class GetRepairTaskByIdQueryHandler(
    ILogger<GetRepairTaskByIdQueryHandler> logger,
    IAppDbContext context)
    : IRequestHandler<GetRepairTaskByIdQuery, Result<RepairTaskDto>>
{
    public async Task<Result<RepairTaskDto>> Handle(GetRepairTaskByIdQuery query, CancellationToken ct)
    {
        var repairTask = await context.RepairTasks
            .AsNoTracking()
            .Include(rt => rt.Parts)
            .FirstOrDefaultAsync(rt => rt.Id == query.RepairTaskId, ct);

        if (repairTask is null)
        {
            logger.LogWarning("Repair task with id {RepairTaskId} was not found", query.RepairTaskId);
            return ApplicationErrors.RepairTasks.NotFound;
        }

        var requestedInventoryIds = repairTask.Parts.Select(p => p.InventoryItemId).Distinct().ToList();

        var inventoryItemsMap = await context.InventoryItems
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(i => requestedInventoryIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        return repairTask.ToDto(inventoryItemsMap);
    }
}