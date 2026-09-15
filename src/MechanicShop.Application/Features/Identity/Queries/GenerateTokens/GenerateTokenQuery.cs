using MechanicShop.Application.Features.Identity.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Identity.Queries.GenerateTokens;

public sealed record GenerateTokenQuery(string Email, string Password) : IRequest<Result<TokenResponse>>;