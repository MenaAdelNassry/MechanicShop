using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Spots.Commands.ToggleSpotActive;

public sealed record ToggleSpotActiveCommand(Guid Id) : IRequest<Result<Success>>;
