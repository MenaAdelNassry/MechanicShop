using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Billing.Commands.CreateOnlinePaymentLink;

public sealed record CreateOnlinePaymentLinkCommand(
    Guid InvoiceId,
    string SuccessUrl,
    string CancelUrl
) : IRequest<Result<CreatePaymentLinkDto>>;