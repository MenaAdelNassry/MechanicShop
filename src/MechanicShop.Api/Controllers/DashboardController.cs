using Asp.Versioning;
using MechanicShop.Application.Features.Dashboard.Dtos;
using MechanicShop.Application.Features.Dashboard.Queries.GetWorkOrderStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicShop.Api.Controllers;

[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/dashboard")]
public sealed class DashboardController(ISender sender) : ApiController
{
    [HttpGet("stats")]
    public async Task<ActionResult<TodayWorkOrderStatsDto>> GetTodayStats(
        [FromQuery] DateOnly? date,
        [FromQuery] string? timeZoneId = "Africa/Cairo",
        CancellationToken ct = default)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var query = new GetWorkOrderStatsQuery(targetDate, timeZoneId);
        var result = await sender.Send(query, ct);

        return result.Match(Ok, Problem);
    }
}