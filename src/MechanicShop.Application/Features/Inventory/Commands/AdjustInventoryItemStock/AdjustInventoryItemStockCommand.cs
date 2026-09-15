using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Inventory.Commands.AdjustInventoryItemStock;

public sealed record AdjustInventoryItemStockCommand(
    Guid InventoryItemId,
    int Quantity,
    string Reason,
    Guid? PerformedBy
) : IRequest<Result<Updated>>;
