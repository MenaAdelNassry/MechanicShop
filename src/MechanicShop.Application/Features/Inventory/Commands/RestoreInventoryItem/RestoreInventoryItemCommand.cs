using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Inventory.Commands.RestoreInventoryItem;
public record RestoreInventoryItemCommand(Guid Id) : IRequest<Result<Updated>>;
