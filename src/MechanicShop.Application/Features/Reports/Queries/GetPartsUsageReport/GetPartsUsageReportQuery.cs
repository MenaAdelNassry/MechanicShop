using MechanicShop.Application.Features.Reports.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Reports.Queries.GetPartsUsageReport;

public sealed record GetPartsUsageReportQuery(
    DateOnly FromDate,
    DateOnly ToDate,
    TimeZoneInfo TimeZone
) : IRequest<Result<PartsUsageReportDto>>;
