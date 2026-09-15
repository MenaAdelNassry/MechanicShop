using MechanicShop.Application.Features.Inventory.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Inventory.Queries.GetArchivedInventoryItems;

public record GetArchivedInventoryItemsQuery : IRequest<Result<List<InventoryItemDto>>>;
