using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Spots.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Spots;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Spots.Commands.CreateSpot;

public sealed class CreateSpotCommandHandler(IAppDbContext context)
    : IRequestHandler<CreateSpotCommand, Result<SpotDto>>
{
    public async Task<Result<SpotDto>> Handle(CreateSpotCommand request, CancellationToken ct)
    {
        var normalizedName = request.Name.Trim();

        var nameExists = await context.ServiceBays
            .AnyAsync(s => s.Name.ToLower() == normalizedName.ToLower(), ct);

        if (nameExists)
        {
            return ApplicationErrors.ServiceBays.DuplicateName;
        }

        var bay = ServiceBay.Create(normalizedName, request.Description);

        context.ServiceBays.Add(bay);
        await context.SaveChangesAsync(ct);

        return new SpotDto(bay.Id, bay.Name, bay.Description, bay.IsActive);
    }
}