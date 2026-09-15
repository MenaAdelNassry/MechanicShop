using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Inventory.Commands.DeleteInventoryItem;

public sealed record DeleteInventoryItemCommand(Guid Id) : IRequest<Result<Deleted>>;