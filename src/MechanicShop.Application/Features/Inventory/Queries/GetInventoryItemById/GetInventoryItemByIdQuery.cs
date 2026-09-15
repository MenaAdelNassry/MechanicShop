using MechanicShop.Application.Features.Inventory.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Inventory.Queries.GetInventoryItemById;

public record GetInventoryItemByIdQuery(Guid Id) : IRequest<Result<InventoryItemDto>>;