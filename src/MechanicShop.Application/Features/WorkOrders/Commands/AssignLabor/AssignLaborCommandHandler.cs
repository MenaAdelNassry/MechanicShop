using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Commands.AssignLabor;

public sealed class AssignLaborCommandHandler(
    ILogger<AssignLaborCommandHandler> logger,
    IAppDbContext context,
    IWorkOrderScheduleReadStore scheduleReadStore)
    : IRequestHandler<AssignLaborCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(AssignLaborCommand command, CancellationToken ct)
    {
        var workOrder = await context.WorkOrders
            .FirstOrDefaultAsync(wo => wo.Id == command.WorkOrderId, ct);

        if (workOrder is null)
        {
            logger.LogError("WorkOrder with Id '{WorkOrderId}' does not exist.", command.WorkOrderId);
            return ApplicationErrors.WorkOrders.NotFound;
        }

        var labor = await context.Employees.FindAsync([command.LaborId], ct);

        if (labor is null)
        {
            logger.LogError("Invalid LaborId: {LaborId}", command.LaborId);
            return ApplicationErrors.WorkOrders.LaborNotFound;
        }

        var hasConflict = await scheduleReadStore.HasLaborConflictAsync(
            command.LaborId,
            workOrder.StartAtUtc.UtcDateTime,
            workOrder.EndAtUtc.UtcDateTime,
            command.WorkOrderId);

        if (hasConflict)
        {
            logger.LogError("Labor with Id '{LaborId}' is already occupied during the requested time.", command.LaborId);
            return ApplicationErrors.WorkOrders.LaborOccupied;
        }

        var updateLaborResult = workOrder.UpdateLabor(command.LaborId);

        if (updateLaborResult.IsError)
        {
            foreach (var error in updateLaborResult.Errors)
            {
                logger.LogError("[LaborUpdate] {ErrorCode}: {ErrorDescription}", error.Code, error.Description);
            }

            return updateLaborResult.Errors;
        }

        await context.SaveChangesAsync(ct);

        return Result.Updated;
    }
}