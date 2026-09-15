using MechanicShop.Application.Features.Inventory.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Inventory.Queries.GetInventoryItems;

public sealed record GetInventoryItemsQuery : IRequest<Result<IReadOnlyList<InventoryItemDto>>>;