using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Application.Features.RepairTasks.Queries.GetRepairTaskById;

public sealed record GetRepairTaskByIdQuery(Guid RepairTaskId) : ICachedQuery<Result<RepairTaskDto>>
{
    public string CacheKey => $"repairtask_{RepairTaskId}";

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);

    public string[] Tags => [$"repairtask_{RepairTaskId}"];
}