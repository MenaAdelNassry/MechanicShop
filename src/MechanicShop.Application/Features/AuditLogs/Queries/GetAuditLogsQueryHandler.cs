using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Common.Models;
using MechanicShop.Application.Features.AuditLogs.Dtos;
using MechanicShop.Application.Features.Billing.Mappers;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.AuditLogs.Queries.GetAuditLogs;

public sealed class GetAuditLogsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetAuditLogsQuery, Result<PaginatedList<AuditLogDto>>>
{
    public async Task<Result<PaginatedList<AuditLogDto>>> Handle(GetAuditLogsQuery query, CancellationToken ct)
    {
        var logsQuery = context.AuditLogs
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.TableName))
        {
            logsQuery = logsQuery.Where(l => l.TableName == query.TableName);
        }

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            logsQuery = logsQuery.Where(l => l.UserId == query.UserId);
        }

        if (!string.IsNullOrWhiteSpace(query.ActionType))
        {
            logsQuery = logsQuery.Where(l => l.Type == query.ActionType);
        }

        if (query.FromDate.HasValue)
        {
            logsQuery = logsQuery.Where(l => l.DateTimeUtc >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            var endOfDay = query.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            logsQuery = logsQuery.Where(l => l.DateTimeUtc <= endOfDay);
        }

        var totalCount = await logsQuery.CountAsync(ct);
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

        var items = await logsQuery
            .OrderByDescending(l => l.DateTimeUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => l.ToDto())
            .ToListAsync(ct);

        return new PaginatedList<AuditLogDto>
        {
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            Items = items
        };
    }
}