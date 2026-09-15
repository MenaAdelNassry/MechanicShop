using MechanicShop.Domain.Common.Results;
using MediatR;

namespace MechanicShop.Application.Features.Identity.Commands.SetPassword;

public sealed record SetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<Result<Updated>>;
