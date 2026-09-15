using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.RepairTasks.Queries.GetDeletedRepairTasks;

public sealed record GetDeletedRepairTasksQuery : IRequest<Result<IReadOnlyList<RepairTaskDto>>>;