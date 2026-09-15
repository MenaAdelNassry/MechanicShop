using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Commands.RelocateWorkOrder;

public sealed class RelocateWorkOrderCommandHandler(
    ILogger<RelocateWorkOrderCommandHandler> logger,
    IAppDbContext context,
    IWorkOrderPolicy workOrderPolicy,
    IWorkOrderScheduleReadStore scheduleReadStore)
    : IRequestHandler<RelocateWorkOrderCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(RelocateWorkOrderCommand command, CancellationToken ct)
    {
        var workOrder = await context.WorkOrders
            .Include(wo => wo.RepairTasks)
            .FirstOrDefaultAsync(a => a.Id == command.WorkOrderId, ct);

        if (workOrder is null)
        {
            logger.LogError("WorkOrder with Id '{WorkOrderId}' does not exist.", command.WorkOrderId);
            return ApplicationErrors.WorkOrders.NotFound;
        }

        if (!workOrder.IsEditable)
        {
            return WorkOrderErrors.Readonly;
        }

        if (workOrderPolicy.IsOutsideOperatingHours(command.NewStartAt))
        {
            return ApplicationErrors.WorkOrders.OutsideOperatingHours(
                command.NewStartAt,
                workOrderPolicy.WorkshopTimeZone,
                workOrderPolicy.OpeningTime,
                workOrderPolicy.ClosingTime);
        }

        var totalMinutes = workOrder.RepairTasks.Sum(t => (int)t.EstimatedDurationInMins);
        var totalDuration = TimeSpan.FromMinutes(totalMinutes);
        var endAt = workOrderPolicy.CalculateActualEndAt(command.NewStartAt, totalDuration);

        var startAtUtc = command.NewStartAt.UtcDateTime;
        var endAtUtc = endAt.UtcDateTime;

        var isSpotBusy = await scheduleReadStore.HasSpotConflictAsync(
            command.NewSpot,
            startAtUtc,
            endAtUtc,
            excludeWorkOrderId: workOrder.Id,
            ct);

        if (isSpotBusy)
        {
            logger.LogError("Spot: {Spot} is not available.", command.NewSpot.ToString());
            return ApplicationErrors.WorkOrders.SpotIsNotAvailable;
        }

        if (workOrder.LaborId != Guid.Empty)
        {
            var isLaborOccupied = await scheduleReadStore.HasLaborConflictAsync(
                workOrder.LaborId,
                startAtUtc,
                endAtUtc,
                excludeWorkOrderId: workOrder.Id,
                ct);

            if (isLaborOccupied)
            {
                logger.LogError("Labor with Id '{LaborId}' is already occupied during the requested time.", workOrder.LaborId);
                return ApplicationErrors.WorkOrders.LaborOccupied;
            }
        }

        var hasVehicleConflict = await scheduleReadStore.HasVehicleConflictAsync(
            workOrder.VehicleId,
            startAtUtc,
            endAtUtc,
            excludeWorkOrderId: workOrder.Id,
            ct);

        if (hasVehicleConflict)
        {
            logger.LogError("Vehicle with Id '{VehicleId}' already has an overlapping WorkOrder.", workOrder.VehicleId);
            return ApplicationErrors.WorkOrders.VehicleSchedulingConflict;
        }

        var updateTimingResult = workOrder.UpdateTiming(command.NewStartAt, endAt);
        if (updateTimingResult.IsError)
        {
            logger.LogError("Failed to update timing: {Error}", updateTimingResult.TopError.Description);
            return updateTimingResult.Errors;
        }

        var updateSpotResult = workOrder.UpdateSpot(command.NewSpot);
        if (updateSpotResult.IsError)
        {
            logger.LogError("Failed to update Spot: {Error}", updateSpotResult.TopError.Description);
            return updateSpotResult.Errors;
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation("WorkOrder '{WorkOrderId}' relocated successfully.", workOrder.Id);

        return Result.Updated;
    }
}