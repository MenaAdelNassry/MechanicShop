using Asp.Versioning;
using MechanicShop.Api.Requests.RepairTasks;
using MechanicShop.Application.Features.RepairTasks.Commands.ActivateRepairTask;
using MechanicShop.Application.Features.RepairTasks.Commands.CreateRepairTask;
using MechanicShop.Application.Features.RepairTasks.Commands.RemoveRepairTask;
using MechanicShop.Application.Features.RepairTasks.Commands.UpdateRepairTask;
using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Application.Features.RepairTasks.Queries.GetDeletedRepairTasks;
using MechanicShop.Application.Features.RepairTasks.Queries.GetRepairTaskById;
using MechanicShop.Application.Features.RepairTasks.Queries.GetRepairTasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicShop.Api.Controllers;

[Route("api/v{version:apiVersion}/repair-tasks")]
[ApiVersion("1.0")]
[Authorize]
public sealed class RepairTasksController(ISender sender) : ApiController
{
    [HttpGet]
    [EndpointSummary("Retrieves all repair tasks.")]
    [EndpointDescription("Returns a list of all repair tasks available in the system.")]
    [EndpointName("GetRepairTasks")]
    public async Task<ActionResult<List<RepairTaskDto>>> Get(CancellationToken ct)
    {
        var result = await sender.Send(new GetRepairTasksQuery(), ct);

        return result.Match(Ok, Problem);
    }

    [HttpGet("{repairTaskId:guid}", Name = "GetRepairTaskById")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieves a repair task by ID.")]
    [EndpointDescription("Returns detailed information for the specified repair task if it exists.")]
    [EndpointName("GetRepairTaskById")]
    public async Task<ActionResult<RepairTaskDto>> GetById(Guid repairTaskId, CancellationToken ct)
    {
        var result = await sender.Send(new GetRepairTaskByIdQuery(repairTaskId), ct);

        return result.Match(Ok, Problem);
    }

    [HttpPost]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(typeof(RepairTaskDto), StatusCodes.Status201Created)]
    [EndpointSummary("Creates a new repair task.")]
    [EndpointDescription("Creates a repair task and optionally includes parts.")]
    [EndpointName("CreateRepairTask")]
    public async Task<IActionResult> Create([FromBody] CreateRepairTaskRequest request, CancellationToken ct)
    {
        var parts = request.Parts
            .ConvertAll(p => new CreateRepairTaskPartCommand(p.InventoryItemId, p.Quantity));

        var command = new CreateRepairTaskCommand(
            request.Name,
            request.LaborCost,
            request.EstimatedDurationInMins,
            parts);

        var result = await sender.Send(command, ct);

        return result.Match(
            response => CreatedAtRoute("GetRepairTaskById", new { repairTaskId = response.OriginalRepairTaskId }, response),
            Problem);
    }

    [HttpPut("{repairTaskId:guid}")]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Updates an existing repair task.")]
    [EndpointDescription("Updates a repair task and its associated parts.")]
    [EndpointName("UpdateRepairTask")]
    public async Task<IActionResult> Update(Guid repairTaskId, [FromBody] UpdateRepairTaskRequest request, CancellationToken ct)
    {
        var parts = request.Parts
            .ConvertAll(p => new UpdateRepairTaskPartCommand(p.InventoryItemId, p.Quantity));

        var command = new UpdateRepairTaskCommand(
            repairTaskId,
            request.Name,
            request.LaborCost,
            request.EstimatedDurationInMins,
            parts);

        var result = await sender.Send(command, ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }

    [HttpDelete("{repairTaskId:guid}")]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Removes a repair task.")]
    [EndpointDescription("Deletes the specified repair task from the system.")]
    [EndpointName("RemoveRepairTask")]
    public async Task<IActionResult> Delete(Guid repairTaskId, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveRepairTaskCommand(repairTaskId), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }

    [HttpGet("deleted")]
    [Authorize(Policy = "ManagerOnly")]
    [EndpointSummary("Get Removed Repair Tasks.")]
    [EndpointDescription("Get Any Reapair Tasks was deleted as soft delete")]
    [EndpointName("GetRemovedRepairTasks")]
    public async Task<ActionResult<List<RepairTaskDto>>> GetDeleted(CancellationToken ct)
    {
        var result = await sender.Send(new GetDeletedRepairTasksQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = "ManagerOnly")]
    [EndpointSummary("Activate a repair task.")]
    [EndpointDescription("Activate a repair task that was previously deactivated.")]
    [EndpointName("ActivateRepairTask")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ActivateRepairTaskCommand(id), ct);
        return result.Match(_ => Ok(), Problem);
    }
}