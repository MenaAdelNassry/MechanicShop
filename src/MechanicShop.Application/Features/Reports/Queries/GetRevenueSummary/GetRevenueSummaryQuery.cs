using MechanicShop.Application.Features.Reports.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Reports.Queries.GetRevenueSummary;

public sealed record GetRevenueSummaryQuery(
    DateOnly FromDate,
    DateOnly ToDate,
    string TimeZone
) : IRequest<Result<RevenueSummaryDto>>;