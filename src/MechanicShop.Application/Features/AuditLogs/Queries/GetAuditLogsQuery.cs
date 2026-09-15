using MechanicShop.Application.Common.Models;
using MechanicShop.Application.Features.AuditLogs.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.AuditLogs.Queries.GetAuditLogs;

public sealed record GetAuditLogsQuery(
    string? TableName = null,
    string? UserId = null,
    string? ActionType = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PaginatedList<AuditLogDto>>>;