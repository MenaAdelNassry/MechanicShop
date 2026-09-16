using MechanicShop.Application.Features.Spots.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Spots.Commands.CreateSpot;

public sealed record CreateSpotCommand(string Name, string? Description)
    : IRequest<Result<SpotDto>>;
