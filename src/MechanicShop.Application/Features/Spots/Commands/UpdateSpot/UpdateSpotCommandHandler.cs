using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Spots.Commands.UpdateSpot;

public sealed class UpdateSpotCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdateSpotCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(UpdateSpotCommand request, CancellationToken ct)
    {
        var bay = await context.ServiceBays
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct);

        if (bay is null)
        {
            return ApplicationErrors.ServiceBays.NotFound;
        }

        var normalizedName = request.Name.Trim();

        var nameExists = await context.ServiceBays
            .AnyAsync(s => s.Id != request.Id && s.Name.ToLower() == normalizedName.ToLower(), ct);

        if (nameExists) return ApplicationErrors.ServiceBays.DuplicateName;

        bay.Update(normalizedName, request.Description);
        await context.SaveChangesAsync(ct);

        return Result.Success;
    }
}