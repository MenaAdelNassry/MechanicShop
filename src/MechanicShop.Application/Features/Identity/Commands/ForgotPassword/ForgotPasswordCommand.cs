using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Identity.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result<Success>>;