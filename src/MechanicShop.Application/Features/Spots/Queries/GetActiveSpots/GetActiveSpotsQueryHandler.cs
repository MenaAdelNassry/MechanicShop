using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Spots.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Spots.Queries.GetActiveSpots;

public sealed class GetActiveSpotsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetActiveSpotsQuery, Result<IReadOnlyList<SpotDto>>>
{
    public async Task<Result<IReadOnlyList<SpotDto>>> Handle(
        GetActiveSpotsQuery request,
        CancellationToken ct)
    {
        var spots = await context.ServiceBays
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SpotDto(s.Id, s.Name, s.Description, s.IsActive))
            .ToListAsync(ct);

        return spots;
    }
}