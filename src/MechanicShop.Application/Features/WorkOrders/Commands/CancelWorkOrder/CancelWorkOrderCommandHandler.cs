using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Commands.CancelWorkOrder;

public sealed class CancelWorkOrderCommandHandler(
    ILogger<CancelWorkOrderCommandHandler> logger,
    IAppDbContext context)
    : IRequestHandler<CancelWorkOrderCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(CancelWorkOrderCommand command, CancellationToken ct)
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

        var cancelResult = workOrder.Cancel();
        if (cancelResult.IsError)
        {
            logger.LogError("Cancellation failed for WorkOrder '{WorkOrderId}'. Current status is {Status}.", workOrder.Id, workOrder.State);
            return cancelResult.Errors;
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation("WorkOrder with Id '{WorkOrderId}' was cancelled successfully.", command.WorkOrderId);

        return Result.Updated;
    }
}