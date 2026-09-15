using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace MechanicShop.Application.Features.RepairTasks.Commands.ActivateRepairTask;

public class ActivateRepairTaskCommandHandler(IAppDbContext context)
    : IRequestHandler<ActivateRepairTaskCommand, Result<Updated>>
{
    public async Task<Result<Updated>> Handle(ActivateRepairTaskCommand command, CancellationToken ct)
    {
        var task = await context.RepairTasks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == command.Id, ct);

        if (task is null)
        {
            return ApplicationErrors.RepairTasks.NotFound;
        }

        var activateResult = task.Activate();
        if (activateResult.IsError)
        {
            return activateResult.Errors;
        }

        await context.SaveChangesAsync(ct);

        return Result.Updated;
    }
}