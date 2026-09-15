using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.RepairTasks;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.RepairTasks.Commands.UpdateRepairTask;

public class UpdateRepairTaskCommandHandler(
    ILogger<UpdateRepairTaskCommandHandler> logger,
    IAppDbContext context
    )
    : IRequestHandler<UpdateRepairTaskCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(UpdateRepairTaskCommand command, CancellationToken ct)
    {
        var repairTask = await context.RepairTasks
            .Include(rt => rt.Parts)
            .FirstOrDefaultAsync(rt => rt.Id == command.RepairTaskId, ct);

        if (repairTask is null)
        {
            logger.LogWarning("RepairTask {RepairTaskId} not found for update.", command.RepairTaskId);
            return ApplicationErrors.RepairTasks.NotFound;
        }

        var nameTaken = await context.RepairTasks.AnyAsync(
            rt => rt.Id != command.RepairTaskId &&
            rt.Name.ToLower() == command.Name.ToLower(), ct);

        if (nameTaken)
        {
            logger.LogWarning("RepairTask name {RepairTaskName} is already taken.", command.Name);
            return ApplicationErrors.RepairTasks.DuplicateName;
        }

        // 1. Validate that all requested InventoryItemIds exist in the database
        var requestedInventoryIds = command.Parts.Select(p => p.InventoryItemId).Distinct().ToList();
        var existingItemsCount = await context.InventoryItems
            .CountAsync(i => requestedInventoryIds.Contains(i.Id), ct);

        if (existingItemsCount != requestedInventoryIds.Count)
        {
            return Error.NotFound("InventoryItem.NotFound", "One or more inventory items do not exist.");
        }

        // 2. Build domain RepairTaskPartData instances
        var partsData = command.Parts
            .Select(p => new RepairTaskPartData(p.InventoryItemId, p.Quantity));

        // 3. Update repair task details
        var updateRepairTaskResult = repairTask.Update(command.Name, command.LaborCost, command.EstimatedDurationInMins);

        if (updateRepairTaskResult.IsError)
        {
            return updateRepairTaskResult.Errors;
        }

        // 4. Upsert updated parts collection
        var upsertPartsResult = repairTask.UpsertParts(partsData);

        if (upsertPartsResult.IsError)
        {
            return upsertPartsResult.Errors;
        }

        await context.SaveChangesAsync(ct);

        return Result.Updated;
    }
}