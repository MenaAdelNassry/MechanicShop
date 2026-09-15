using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Application.Features.WorkOrders.Mappers;
using MechanicShop.Application.Metrices;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Employees;
using MechanicShop.Domain.Workorders;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Commands.CreateWorkOrder;

public sealed class CreateWorkOrderCommandHandler(
    ILogger<CreateWorkOrderCommandHandler> logger,
    IAppDbContext context,
    IWorkOrderPolicy workOrderPolicy,
    IWorkOrderScheduleReadStore scheduleReadStore)
    : IRequestHandler<CreateWorkOrderCommand, Result<WorkOrderDto>>
{
    public async Task<Result<WorkOrderDto>> Handle(CreateWorkOrderCommand command, CancellationToken ct)
    {
        // 1. Check if the requested start time is within operating hours
        if (workOrderPolicy.IsOutsideOperatingHours(command.StartAt))
        {
            return ApplicationErrors.WorkOrders.OutsideOperatingHours(
                command.StartAt,
                workOrderPolicy.WorkshopTimeZone,
                workOrderPolicy.OpeningTime,
                workOrderPolicy.ClosingTime);
        }

        // 2. Fetch the required repair tasks
        var repairTasks = await context.RepairTasks
            .Include(t => t.Parts)
            .Where(t => command.RepairTaskIds.Contains(t.Id))
            .ToListAsync(ct);

        if (repairTasks.Count != command.RepairTaskIds.Count)
        {
            var missingIds = command.RepairTaskIds.Except(repairTasks.Select(t => t.Id)).ToArray();
            logger.LogError("Some RepairTaskIds not found: {MissingIds}", string.Join(", ", missingIds));
            return ApplicationErrors.RepairTasks.NotFound;
        }

        // 3. Calculate the total estimated duration and determine the actual end time accurately across days
        var totalEstimatedDuration = TimeSpan.FromMinutes(repairTasks.Sum(r => (int)r.EstimatedDurationInMins));
        var endAt = workOrderPolicy.CalculateActualEndAt(command.StartAt, totalEstimatedDuration);

        var startAtUtc = command.StartAt.UtcDateTime;
        var endAtUtc = endAt.UtcDateTime;

        // 4. (Spot Conflict)
        var isSpotBusy = await scheduleReadStore.HasSpotConflictAsync(command.Spot, startAtUtc, endAtUtc, null, ct);
        if (isSpotBusy)
        {
            logger.LogError("Spot: {Spot} is not available during the requested period.", command.Spot.ToString());
            return ApplicationErrors.WorkOrders.SpotNotAvailable(command.Spot, command.StartAt, endAt);
        }

        // 5. (Vehicle Conflict)
        var vehicle = await context.Vehicles
            .Include(v => v.Customer)
            .FirstOrDefaultAsync(v => v.Id == command.VehicleId, ct);

        if (vehicle is null) return ApplicationErrors.Vehicles.NotFound;

        var hasVehicleConflict = await scheduleReadStore.HasVehicleConflictAsync(command.VehicleId, startAtUtc, endAtUtc, null, ct);
        if (hasVehicleConflict)
        {
            logger.LogError("Vehicle with Id '{VehicleId}' already has an overlapping WorkOrder.", command.VehicleId);
            return ApplicationErrors.Vehicles.SchedulingConflict;
        }

        // 6. (Labor Conflict)
        Employee? labor = null;
        if (command.LaborId.HasValue && command.LaborId.Value != Guid.Empty)
        {
            labor = await context.Employees.FindAsync([command.LaborId.Value], ct);
            if (labor is null) return ApplicationErrors.WorkOrders.LaborNotFound;

            var isLaborOccupied = await scheduleReadStore.HasLaborConflictAsync(labor.Id, startAtUtc, endAtUtc, null, ct);
            if (isLaborOccupied)
            {
                logger.LogError("Labor with Id '{LaborId}' is already occupied during the requested time.", labor.Id);
                return ApplicationErrors.WorkOrders.LaborOccupied;
            }
        }

        // 7. (Parts Fetch) To avoid N+1 queries, fetch all required InventoryItems in a single query and create a map for quick access
        var allPartIds = repairTasks
            .SelectMany(rt => rt.Parts)
            .Select(p => p.InventoryItemId)
            .Distinct()
            .ToList();

        var inventoryItemsMap = await context.InventoryItems
            .Where(i => allPartIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, ct);

        var workOrderId = Guid.CreateVersion7();
        var workOrderSnapshots = new List<WorkOrderTask>();

        foreach (var rt in repairTasks)
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

                var reserveResult = inventoryItem.ReserveStock(p.Quantity, workOrderId, taskId);
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

            workOrderSnapshots.Add(taskSnapshot);
        }

        // 8. (Create WorkOrder)
        var createWorkOrderResult = WorkOrder.Create(
            workOrderId,
            command.VehicleId,
            command.StartAt,
            endAt,
            labor?.Id ?? Guid.Empty,
            command.Spot,
            workOrderSnapshots);

        if (createWorkOrderResult.IsError)
        {
            logger.LogError("Failed to create WorkOrder: {Error}", createWorkOrderResult.TopError.Description);
            return createWorkOrderResult.Errors;
        }

        var workOrder = createWorkOrderResult.Value;

        context.WorkOrders.Add(workOrder);
        await context.SaveChangesAsync(ct);

        MechanicShopMetrics.WorkOrdersCreated.Add(1);
        logger.LogInformation("WorkOrder with Id '{WorkOrderId}' created successfully.", workOrder.Id);

        return workOrder.ToDto(vehicle, labor);
    }
}