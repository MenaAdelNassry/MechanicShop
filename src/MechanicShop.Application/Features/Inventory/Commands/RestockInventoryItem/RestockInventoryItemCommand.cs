using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Inventory.Commands.RestockInventoryItem;

public sealed record RestockInventoryItemCommand(
    Guid InventoryItemId,
    int Quantity
) : IRequest<Result<Updated>>;
