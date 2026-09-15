using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.RepairTasks.Commands.ActivateRepairTask;

public record ActivateRepairTaskCommand(Guid Id) : IRequest<Result<Updated>>;