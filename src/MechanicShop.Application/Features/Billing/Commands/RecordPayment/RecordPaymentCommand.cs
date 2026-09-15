using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Billing.Enums;

using MediatR;

namespace MechanicShop.Application.Features.Billing.Commands.RecordPayment;

public sealed record RecordPaymentCommand(
    Guid InvoiceId,
    decimal Amount,
    PaymentMethod Method,
    string? TransactionReference = null
) : IRequest<Result<PaymentDto>>;