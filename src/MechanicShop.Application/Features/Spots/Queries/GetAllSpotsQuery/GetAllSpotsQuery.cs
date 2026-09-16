using MechanicShop.Application.Features.Spots.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Spots.Queries.GetAllSpots;

public sealed record GetAllSpotsQuery : IRequest<Result<IReadOnlyList<SpotDto>>>;
