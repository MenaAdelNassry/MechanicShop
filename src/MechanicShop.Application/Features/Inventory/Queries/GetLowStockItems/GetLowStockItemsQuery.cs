using MechanicShop.Application.Features.Inventory.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Inventory.Queries.GetLowStockItems;

public record GetLowStockItemsQuery : IRequest<Result<List<InventoryItemDto>>>;