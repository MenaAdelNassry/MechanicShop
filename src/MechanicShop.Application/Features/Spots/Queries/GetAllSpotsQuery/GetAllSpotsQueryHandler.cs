using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Spots.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Spots.Queries.GetAllSpots;

public sealed class GetAllSpotsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetAllSpotsQuery, Result<IReadOnlyList<SpotDto>>>
{
    public async Task<Result<IReadOnlyList<SpotDto>>> Handle(
        GetAllSpotsQuery request,
        CancellationToken ct)
    {
        var spots = await context.ServiceBays
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SpotDto(s.Id, s.Name, s.Description, s.IsActive))
            .ToListAsync(ct);

        return spots;
    }
}