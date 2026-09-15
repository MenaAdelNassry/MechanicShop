using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Application.Features.RepairTasks.Queries.GetRepairTasks;

public sealed record GetRepairTasksQuery : ICachedQuery<Result<List<RepairTaskDto>>>
{
    public string CacheKey => "repairtask_list";

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);

    public string[] Tags => ["repairtask_list"];
}