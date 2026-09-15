using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Inventory.Commands.UpdateInventoryItem;

public sealed record UpdateInventoryItemCommand(
    Guid InventoryItemId,
    string Name,
    decimal Cost,
    int ReorderLevel
) : IRequest<Result<Updated>>;
