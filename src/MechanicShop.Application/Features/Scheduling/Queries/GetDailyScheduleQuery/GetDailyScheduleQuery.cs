using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Scheduling.Dtos;
using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Application.Features.Scheduling.Queries.GetDailyScheduleQuery;

public sealed record GetDailyScheduleQuery(
    string? TimeZone,
    DateOnly? ScheduleDate,
    int SlotDurationInMinutes = 15,
    Guid? LaborId = null) : ICachedQuery<Result<ScheduleDto>>
{
    public string CacheKey => $"workorder_schedule_{ScheduleDate:yyyy-MM-dd}_dur={SlotDurationInMinutes}_labor={LaborId?.ToString() ?? "all"}";

    public string[] Tags => ["workorder_list"];

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);
}