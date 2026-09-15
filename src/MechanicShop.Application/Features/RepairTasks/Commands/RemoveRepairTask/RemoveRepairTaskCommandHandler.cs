using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.RepairTasks.Commands.RemoveRepairTask;

public sealed class RemoveRepairTaskCommandHandler(
    ILogger<RemoveRepairTaskCommandHandler> logger,
    IAppDbContext context)
    : IRequestHandler<RemoveRepairTaskCommand, Result<Deleted>>
{
    public async Task<Result<Deleted>> Handle(RemoveRepairTaskCommand command, CancellationToken ct)
    {
        var repairTask = await context.RepairTasks
            .FirstOrDefaultAsync(rt => rt.Id == command.RepairTaskId, ct);

        if (repairTask is null)
        {
            logger.LogWarning("RepairTask {RepairTaskId} not found for deletion.", command.RepairTaskId);
            return ApplicationErrors.RepairTasks.NotFound;
        }

        var deleteResult = repairTask.Delete();
        if (deleteResult.IsError)
        {
            return deleteResult.Errors;
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation("RepairTask {RepairTaskId} deleted successfully.", command.RepairTaskId);

        return Result.Deleted;
    }
}