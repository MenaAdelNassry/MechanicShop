using Asp.Versioning;

using MechanicShop.Api.Extensions;
using MechanicShop.Api.Requests.Inventory;
using MechanicShop.Application.Features.Inventory.Commands.AdjustInventoryItemStock;
using MechanicShop.Application.Features.Inventory.Commands.CreateInventoryItem;
using MechanicShop.Application.Features.Inventory.Commands.DeleteInventoryItem;
using MechanicShop.Application.Features.Inventory.Commands.RestockInventoryItem;
using MechanicShop.Application.Features.Inventory.Commands.RestoreInventoryItem;
using MechanicShop.Application.Features.Inventory.Commands.UpdateInventoryItem;
using MechanicShop.Application.Features.Inventory.Dtos;
using MechanicShop.Application.Features.Inventory.Queries.GetArchivedInventoryItems;
using MechanicShop.Application.Features.Inventory.Queries.GetInventoryItemById;
using MechanicShop.Application.Features.Inventory.Queries.GetInventoryItems;
using MechanicShop.Application.Features.Inventory.Queries.GetInventoryItemTransactions;
using MechanicShop.Application.Features.Inventory.Queries.GetLowStockItems;
using MechanicShop.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicShop.Api.Controllers;

[Route("api/v{version:apiVersion}/inventory")]
[ApiVersion("1.0")]
[Authorize(Policy = "InventoryManagementAccess")]
public sealed class InventoryController(ISender sender) : ApiController
{
    [HttpGet]
    [EndpointSummary("Get all inventory items")]
    public async Task<ActionResult<List<InventoryItemDto>>> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetInventoryItemsQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("low-stock")]
    [EndpointSummary("Get low stock inventory items")]
    public async Task<ActionResult<List<InventoryItemDto>>> GetLowStock(CancellationToken ct)
    {
        var result = await sender.Send(new GetLowStockItemsQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{id:guid}", Name = nameof(GetInventoryItemById))]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get inventory item by ID")]
    public async Task<ActionResult<InventoryItemDto>> GetInventoryItemById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetInventoryItemByIdQuery(id), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost]
    [ProducesResponseType(typeof(InventoryItemDto), StatusCodes.Status201Created)]
    [EndpointSummary("Add new inventory item")]
    public async Task<IActionResult> Create([FromBody] CreateInventoryItemRequest request, CancellationToken ct)
    {
        var command = new CreateInventoryItemCommand(request.Name, request.Cost, request.StockQuantity, request.ReorderLevel);
        var result = await sender.Send(command, ct);

        return result.Match(
            item => CreatedAtRoute(nameof(GetInventoryItemById), new { id = item.Id }, item),
            Problem);
    }

    [HttpPost("{id:guid}/restock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Restock inventory item quantity")]
    public async Task<IActionResult> Restock(Guid id, [FromBody] RestockInventoryItemRequest request, CancellationToken ct)
    {
        var command = new RestockInventoryItemCommand(id, request.Quantity);
        var result = await sender.Send(command, ct);

        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/adjust")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Adjust inventory item stock manually")]
    public async Task<IActionResult> Adjust(Guid id, [FromBody] AdjustInventoryStockRequest request, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var command = new AdjustInventoryItemStockCommand(id, request.QuantityAdjustment, request.Reason, userId);
        var result = await sender.Send(command, ct);

        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInventoryItemRequest request, CancellationToken ct)
    {
        var command = new UpdateInventoryItemCommand(id, request.Name, request.Cost, request.ReorderLevel);
        var result = await sender.Send(command, ct);

        return result.Match(_ => NoContent(), Problem);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Soft delete an inventory item")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteInventoryItemCommand(id), ct);

        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPost("{id:guid}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [EndpointSummary("Retrieve a soft-deleted / archived inventory item")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new RestoreInventoryItemCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpGet("archived")]
    [ProducesResponseType(typeof(List<InventoryItemDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieves all soft-deleted / archived inventory items.")]
    public async Task<IActionResult> GetArchived(CancellationToken ct)
    {
        var result = await sender.Send(new GetArchivedInventoryItemsQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{id:guid}/transactions")]
    [EndpointSummary("Get transaction audit history for a specific inventory item")]
    public async Task<ActionResult<List<InventoryTransactionDto>>> GetTransactions(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetInventoryItemTransactionsQuery(id), ct);
        return result.Match(Ok, Problem);
    }
}