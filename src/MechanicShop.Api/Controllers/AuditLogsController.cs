using Asp.Versioning;

using MechanicShop.Application.Common.Models;
using MechanicShop.Application.Features.AuditLogs.Dtos;
using MechanicShop.Application.Features.AuditLogs.Queries.GetAuditLogs;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicShop.Api.Controllers;

[Route("api/v{version:apiVersion}/audit-logs")]
[ApiVersion("1.0")]
[Authorize(Policy = "ManagerOnly")]
public sealed class AuditLogsController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedList<AuditLogDto>>> GetAuditLogs(
        [FromQuery] string? tableName,
        [FromQuery] string? userId,
        [FromQuery] string? actionType,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] PageRequest pageRequest,
        CancellationToken ct = default)
    {
        var query = new GetAuditLogsQuery(
            TableName: tableName,
            UserId: userId,
            ActionType: actionType,
            FromDate: fromDate,
            ToDate: toDate,
            PageNumber: pageRequest.Page,
            PageSize: pageRequest.PageSize);

        var result = await sender.Send(query, ct);

        return result.Match(Ok, Problem);
    }
}