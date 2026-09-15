using MechanicShop.Application.Features.Reports.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Reports.Queries.GetTechnicianProductivity;

public sealed record GetTechnicianProductivityQuery(
    DateOnly FromDate,
    DateOnly ToDate,
    TimeZoneInfo TimeZone,
    Guid? LaborId = null
) : IRequest<Result<TechniciansReportDto>>;
