using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Application.Features.RepairTasks.Mappers;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.RepairTasks;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.RepairTasks.Commands.CreateRepairTask;

public class CreateRepairTaskCommandHandler(
        ILogger<CreateRepairTaskCommandHandler> logger,
        IAppDbContext context
    )
    : IRequestHandler<CreateRepairTaskCommand, Result<RepairTaskDto>>
{
    public async Task<Result<RepairTaskDto>> Handle(CreateRepairTaskCommand command, CancellationToken ct)
    {
        var nameExists = await context.RepairTasks
           .AnyAsync(p => p.Name.ToLower() == command.Name.ToLower(), ct);

        if (nameExists)
        {
            logger.LogWarning("Duplicate repair task name '{TaskName}'.", command.Name);
            return ApplicationErrors.RepairTasks.DuplicateName;
        }

        // 1. Validate that all requested InventoryItemIds exist in DB
        var requestedInventoryIds = command.Parts.Select(p => p.InventoryItemId).Distinct().ToList();
        var inventoryItemsMap = await context.InventoryItems
            .Where(i => requestedInventoryIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        if (inventoryItemsMap.Count != requestedInventoryIds.Count)
        {
            return Error.NotFound("InventoryItem.NotFound", "One or more inventory items do not exist.");
        }

        // 2. Build domain RepairTaskPart instances
        var partsData = command.Parts
            .Select(p => new RepairTaskPartData(p.InventoryItemId, p.Quantity));

        // 3. Create domain aggregate
        var createRepairTaskResult = RepairTask.Create(
            id: Guid.CreateVersion7(),
            name: command.Name!,
            laborCost: command.LaborCost,
            estimatedDurationInMins: command.EstimatedDurationInMins!.Value,
            parts: partsData);

        if (createRepairTaskResult.IsError)
        {
            return createRepairTaskResult.Errors;
        }

        var repairTask = createRepairTaskResult.Value;

        context.RepairTasks.Add(repairTask);

        await context.SaveChangesAsync(ct);

        return repairTask.ToDto(inventoryItemsMap);
    }
}