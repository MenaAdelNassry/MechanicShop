using MechanicShop.Application.Features.Spots.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Spots.Queries.GetActiveSpots;

public sealed record GetActiveSpotsQuery : IRequest<Result<IReadOnlyList<SpotDto>>>;
