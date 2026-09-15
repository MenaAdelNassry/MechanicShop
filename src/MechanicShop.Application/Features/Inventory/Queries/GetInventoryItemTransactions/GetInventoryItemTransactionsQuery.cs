using MechanicShop.Application.Features.Inventory.Dtos;
using MechanicShop.Domain.Common.Results;

using MediatR;

namespace MechanicShop.Application.Features.Inventory.Queries.GetInventoryItemTransactions;

public sealed record GetInventoryItemTransactionsQuery(Guid InventoryItemId)
    : IRequest<Result<IReadOnlyList<InventoryTransactionDto>>>;