using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Billing.Commands.ProcessStripeWebhook;

public sealed record ProcessStripeWebhookCommand(
    string Payload,
    string SignatureHeader
) : IRequest<Result<Success>>;