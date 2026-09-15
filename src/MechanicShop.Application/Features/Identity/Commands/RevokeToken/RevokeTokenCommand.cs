using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Identity.Commands.RevokeToken;

public sealed record RevokeTokenCommand(string RefreshToken) : IRequest<Result<Updated>>;