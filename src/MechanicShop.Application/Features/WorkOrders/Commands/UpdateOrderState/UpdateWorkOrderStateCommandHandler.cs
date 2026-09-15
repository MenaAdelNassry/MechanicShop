using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Commands.UpdateOrderState;

public sealed class UpdateWorkOrderStateCommandHandler(
    ILogger<UpdateWorkOrderStateCommandHandler> logger,
    IAppDbContext context,
    TimeProvider dateTime)
    : IRequestHandler<UpdateWorkOrderStateCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(UpdateWorkOrderStateCommand command, CancellationToken ct)
    {
        var workOrder = await context.WorkOrders
            .Include(wo => wo.RepairTasks)
                .ThenInclude(t => t.Parts)
            .FirstOrDefaultAsync(wo => wo.Id == command.WorkOrderId, ct);

        if (workOrder is null)
        {
            logger.LogError("WorkOrder with Id '{WorkOrderId}' does not exist.", command.WorkOrderId);
            return ApplicationErrors.WorkOrders.NotFound;
        }

        var updateStatusResult = workOrder.UpdateState(command.State, dateTime);

        if (updateStatusResult.IsError)
        {
            logger.LogError(
                "Failed to update status for WorkOrder '{WorkOrderId}': {Error}",
                command.WorkOrderId,
                updateStatusResult.TopError.Description);

            return updateStatusResult.Errors;
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "WorkOrder with Id '{WorkOrderId}' successfully updated to state '{State}'.",
            command.WorkOrderId,
            command.State);

        return Result.Updated;
    }
}