using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Dashboard.Dtos;
using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Application.Features.Dashboard.Queries.GetWorkOrderStats;

public sealed record GetWorkOrderStatsQuery(
    DateOnly Date,
    string? TimeZoneId = "Africa/Cairo"
) : ICachedQuery<Result<TodayWorkOrderStatsDto>>
{
    public string CacheKey => $"dashboard:stats:{Date:yyyyMMdd}:{TimeZoneId}";

    public TimeSpan Expiration => TimeSpan.FromMinutes(5);

    public string[] Tags => ["workorder_list", "invoice_list"];
}