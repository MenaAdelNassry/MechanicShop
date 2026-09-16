using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Spots.Commands.ToggleSpotActive;

public sealed class ToggleSpotActiveCommandHandler(IAppDbContext context)
    : IRequestHandler<ToggleSpotActiveCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ToggleSpotActiveCommand request, CancellationToken ct)
    {
        var bay = await context.ServiceBays
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (bay is null)
        {
            return ApplicationErrors.ServiceBays.NotFound;
        }

        if (bay.IsActive)
        {
            var utcNow = DateTimeOffset.UtcNow;
            var hasActiveOrders = await context.WorkOrders
                .AnyAsync(
                    w => w.SpotId == bay.Id &&
                    w.State != WorkOrderState.Cancelled &&
                    w.State != WorkOrderState.Completed &&
                    w.EndAtUtc > utcNow, ct);

            if (hasActiveOrders) return ApplicationErrors.ServiceBays.HasActiveWorkOrders;
        }

        bay.ToggleActive();
        await context.SaveChangesAsync(ct);

        return Result.Success;
    }
}