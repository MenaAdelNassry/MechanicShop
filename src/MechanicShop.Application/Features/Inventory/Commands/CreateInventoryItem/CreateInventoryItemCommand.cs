using MechanicShop.Application.Features.Inventory.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Inventory.Commands.CreateInventoryItem;

public sealed record CreateInventoryItemCommand(
    string Name,
    decimal Cost,
    int InitialStock,
    int ReorderLevel
) : IRequest<Result<InventoryItemDto>>;
