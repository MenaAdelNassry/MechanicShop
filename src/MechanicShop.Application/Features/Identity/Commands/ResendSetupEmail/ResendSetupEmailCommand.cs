using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Identity.Commands.ResendSetupEmail;

public sealed record ResendSetupEmailCommand(string Email) : IRequest<Result<Success>>;