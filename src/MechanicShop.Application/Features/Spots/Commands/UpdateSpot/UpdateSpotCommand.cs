using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Spots.Commands.UpdateSpot;

public sealed record UpdateSpotCommand(Guid Id, string Name, string? Description)
    : IRequest<Result<Success>>;
