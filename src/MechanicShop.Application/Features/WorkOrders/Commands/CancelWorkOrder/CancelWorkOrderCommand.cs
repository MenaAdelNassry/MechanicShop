using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.WorkOrders.Commands.CancelWorkOrder;

public sealed record CancelWorkOrderCommand(Guid WorkOrderId) : IRequest<Result<Updated>>;