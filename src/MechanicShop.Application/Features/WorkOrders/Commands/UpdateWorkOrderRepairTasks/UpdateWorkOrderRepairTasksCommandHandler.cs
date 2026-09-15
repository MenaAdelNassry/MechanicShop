using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Commands.UpdateWorkOrderRepairTasks;

public sealed class UpdateWorkOrderRepairTasksCommandHandler(
    ILogger<UpdateWorkOrderRepairTasksCommandHandler> logger,
    IAppDbContext context,
    IWorkOrderPolicy workOrderPolicy,
    IWorkOrderScheduleReadStore scheduleReadStore)
    : IRequestHandler<UpdateWorkOrderRepairTasksCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(UpdateWorkOrderRepairTasksCommand command, CancellationToken ct)
    {
        // 1. Fetch the work order along with its repair tasks and parts
        var workOrder = await context.WorkOrders
            .Include(w => w.RepairTasks)
                .ThenInclude(t => t.Parts)
            .FirstOrDefaultAsync(w => w.Id == command.WorkOrderId, ct);

        if (workOrder is null)
        {
            logger.LogError("WorkOrder with Id '{WorkOrderId}' does not exist.", command.WorkOrderId);
            return ApplicationErrors.WorkOrders.NotFound;
        }

        if (!workOrder.IsEditable)
        {
            return WorkOrderErrors.Readonly;
        }

        // 2. Fetch the requested new repair tasks
        var requestedTasks = await context.RepairTasks
            .Include(t => t.Parts)
            .Where(t => command.RepairTaskIds.Contains(t.Id))
            .ToListAsync(ct);

        if (requestedTasks.Count != command.RepairTaskIds.Length)
        {
            var missingIds = command.RepairTaskIds.Except(requestedTasks.Select(t => t.Id)).ToArray();
            logger.LogError("One or more RepairTasks not found. {ids}", string.Join(", ", missingIds));
            return ApplicationErrors.RepairTasks.NotFound;
        }

        // 3. Calculate the total duration and the actual end time based on work shifts
        var totalDuration = TimeSpan.FromMinutes(requestedTasks.Sum(x => (int)x.EstimatedDurationInMins));
        var newEndAt = workOrderPolicy.CalculateActualEndAt(workOrder.StartAtUtc, totalDuration);

        var startAtUtc = workOrder.StartAtUtc.UtcDateTime;
        var endAtUtc = newEndAt.UtcDateTime;

        if (workOrderPolicy.IsOutsideOperatingHours(workOrder.StartAtUtc))
        {
            return ApplicationErrors.WorkOrders.OutsideOperatingHours(
                workOrder.StartAtUtc,
                workOrderPolicy.WorkshopTimeZone,
                workOrderPolicy.OpeningTime,
                workOrderPolicy.ClosingTime);
        }

        // 4. Check for spot conflict
        var isSpotBusy = await scheduleReadStore.HasSpotConflictAsync(
            workOrder.Spot,
            startAtUtc,
            endAtUtc,
            excludeWorkOrderId: workOrder.Id,
            ct: ct);

        if (isSpotBusy)
        {
            return ApplicationErrors.WorkOrders.SpotIsNotAvailable;
        }

        // 5. Check for labor conflict
        if (workOrder.LaborId != Guid.Empty)
        {
            var isLaborOccupied = await scheduleReadStore.HasLaborConflictAsync(
                workOrder.LaborId,
                startAtUtc,
                endAtUtc,
                excludeWorkOrderId: workOrder.Id,
                ct: ct);

            if (isLaborOccupied)
            {
                return ApplicationErrors.WorkOrders.LaborOccupied;
            }
        }

        // 6. Check for vehicle conflict
        var hasVehicleConflict = await scheduleReadStore.HasVehicleConflictAsync(
            workOrder.VehicleId,
            startAtUtc,
            endAtUtc,
            excludeWorkOrderId: workOrder.Id,
            ct: ct);

        if (hasVehicleConflict)
        {
            return ApplicationErrors.WorkOrders.VehicleSchedulingConflict;
        }

        // 7. Fetch all inventory items (old and new) in a single query to avoid N+1
        var allInventoryItemIds = workOrder.RepairTasks
            .SelectMany(t => t.Parts.Select(p => p.InventoryItemId))
            .Concat(requestedTasks.SelectMany(t => t.Parts.Select(p => p.InventoryItemId)))
            .Distinct()
            .ToList();

        var inventoryItemsMap = await context.InventoryItems
            .Where(i => allInventoryItemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        // 8. Release old inventory items (RELEASE STOCKS)
        foreach (var oldTask in workOrder.RepairTasks)
        {
            foreach (var oldPart in oldTask.Parts)
            {
                if (inventoryItemsMap.TryGetValue(oldPart.InventoryItemId, out var inventoryItem))
                {
                    inventoryItem.ReleaseStock(oldPart.Quantity, workOrder.Id, oldTask.Id);
                }
            }
        }

        workOrder.ClearRepairTasks();

        // 9. Reserve new inventory items and create snapshots
        foreach (var rt in requestedTasks)
        {
            var partsSnapshot = new List<WorkOrderTaskPart>();
            var taskId = Guid.CreateVersion7();

            foreach (var p in rt.Parts)
            {
                if (!inventoryItemsMap.TryGetValue(p.InventoryItemId, out var inventoryItem))
                {
                    logger.LogError("InventoryItem with Id '{PartId}' not found.", p.InventoryItemId);
                    return ApplicationErrors.Inventory.NotFound;
                }

                var reserveResult = inventoryItem.ReserveStock(p.Quantity, workOrder.Id, taskId);
                if (reserveResult.IsError)
                {
                    return reserveResult.Errors;
                }

                var partSnapshot = new WorkOrderTaskPart(inventoryItem.Id, inventoryItem.Name, inventoryItem.Cost, p.Quantity);
                partsSnapshot.Add(partSnapshot);
            }

            var taskSnapshot = new WorkOrderTask(
                id: taskId,
                originalTaskId: rt.Id,
                name: rt.Name,
                laborCost: rt.LaborCost,
                estimatedDurationInMins: rt.EstimatedDurationInMins,
                parts: partsSnapshot);

            var addTaskResult = workOrder.AddRepairTask(taskSnapshot);
            if (addTaskResult.IsError)
            {
                return addTaskResult.Errors;
            }
        }

        // 10. Update the timing with the calculated end time
        var updateWorkOrderTiming = workOrder.UpdateTiming(workOrder.StartAtUtc, newEndAt);
        if (updateWorkOrderTiming.IsError)
        {
            return updateWorkOrderTiming.Errors;
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Repair tasks for WorkOrder '{WorkOrderId}' updated successfully.", workOrder.Id);

        return Result.Updated;
    }
}