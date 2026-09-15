using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Billing.Queries.GetDailyCashDrawerSummary;

public sealed record GetDailyCashDrawerSummaryQuery(
    DateOnly Date,
    string? TimeZoneId = "Africa/Cairo"
) : IRequest<Result<CashDrawerSummaryDto>>;