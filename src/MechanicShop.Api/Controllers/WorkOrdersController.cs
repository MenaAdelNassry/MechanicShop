using Asp.Versioning;

using MechanicShop.Api.Requests.WorkOrders;
using MechanicShop.Application.Common.Models;
using MechanicShop.Application.Features.Scheduling.Dtos;
using MechanicShop.Application.Features.Scheduling.Queries.GetDailyScheduleQuery;
using MechanicShop.Application.Features.WorkOrders.Commands.AssignLabor;
using MechanicShop.Application.Features.WorkOrders.Commands.CancelWorkOrder;
using MechanicShop.Application.Features.WorkOrders.Commands.CreateWorkOrder;
using MechanicShop.Application.Features.WorkOrders.Commands.RelocateWorkOrder;
using MechanicShop.Application.Features.WorkOrders.Commands.UpdateOrderState;
using MechanicShop.Application.Features.WorkOrders.Commands.UpdateWorkOrderRepairTasks;
using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Application.Features.WorkOrders.Queries.GetWorkOrderByIdQuery;
using MechanicShop.Application.Features.WorkOrders.Queries.GetWorkOrders;
using MechanicShop.Domain.Identity;
using MechanicShop.Domain.Workorders.Enums;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicShop.Api.Controllers;

[Route("api/v{version:apiVersion}/workorders")]
[ApiVersion("1.0")]
[Authorize]
public sealed class WorkOrdersController(ISender sender) : ApiController
{
    [HttpGet]
    [EndpointSummary("Retrieves a paginated list of work orders.")]
    [EndpointDescription("Supports filtering by date range, status, vehicle, labor, spot, and searching by term. Pagination and sorting are supported.")]
    [EndpointName("GetWorkOrders")]
    public async Task<ActionResult<PaginatedList<WorkOrderListItemDto>>> Get([FromQuery] WorkOrderFilterRequest filters, [FromQuery] PageRequest pageRequest, CancellationToken ct)
    {
        var query = new GetWorkOrdersQuery(
            pageRequest.Page,
            pageRequest.PageSize,
            filters.SearchTerm,
            filters.SortColumn,
            filters.SortDirection,
            filters.State is not null ? (WorkOrderState)(int)filters.State : null,
            filters.VehicleId,
            filters.LaborId,
            filters.StartDateFrom,
            filters.StartDateTo,
            filters.EndDateFrom,
            filters.EndDateTo,
            filters.Spot is not null ? (Spot)(int)filters.Spot : null);

        var result = await sender.Send(query, ct);

        return result.Match(Ok, Problem);
    }

    [HttpGet("{workOrderId:guid}", Name = "GetWorkOrderById")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieves a work order by its ID.")]
    [EndpointDescription("Returns detailed information about the specified work order if it exists.")]
    [EndpointName("GetWorkOrderById")]
    public async Task<ActionResult<WorkOrderDto>> GetById(Guid workOrderId, CancellationToken ct)
    {
        var result = await sender.Send(new GetWorkOrderByIdQuery(workOrderId), ct);

        return result.Match(Ok, Problem);
    }

    [HttpPost]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(typeof(WorkOrderDto), StatusCodes.Status201Created)]
    [EndpointSummary("Creates a new work order.")]
    [EndpointDescription("Creates a new work order for a vehicle, specifying labor, tasks, and other required information.")]
    [EndpointName("CreateWorkOrder")]
    public async Task<IActionResult> Create([FromBody] CreateWorkOrderRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateWorkOrderCommand(
            (Spot)(int)request.Spot,
            request.VehicleId,
            request.StartAtUtc,
            request.RepairTaskIds,
            request.LaborId),
            ct);

        return result.Match(
            response => CreatedAtRoute(
                routeName: "GetWorkOrderById",
                routeValues: new { workOrderId = response.WorkOrderId },
                value: response),
            Problem);
    }

    [HttpPut("{workOrderId:guid}/relocation")]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Relocates a work order to a new time and spot.")]
    [EndpointDescription("Updates the scheduled time and assigned bay for a work order. Only users with the Manager role can perform this action.")]
    [EndpointName("RescheduleWorkOrder")]
    public async Task<IActionResult> Relocate(Guid workOrderId, [FromBody] RelocateWorkOrderRequest request, CancellationToken ct)
    {
        var command = new RelocateWorkOrderCommand(
            workOrderId,
            request.NewStartAtUtc,
            (Spot)(int)request.NewSpot);

        var result = await sender.Send(command, ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }

    [HttpPut("{workOrderId:guid}/labor")]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Assigns a labor to a work order.")]
    [EndpointDescription("Associates a labor definition with a specific work order. Only managers can perform this operation.")]
    [EndpointName("AssignLaborToWorkOrder")]
    public async Task<IActionResult> AssignLabor(Guid workOrderId, [FromBody] AssignLaborRequest request, CancellationToken ct)
    {
        var command = new AssignLaborCommand(workOrderId, request.LaborId);

        var result = await sender.Send(command, ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }

    [HttpPut("{workOrderId:guid}/state")]
    [Authorize(Policy = "SelfScopedWorkOrderAccess")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Changes the state of a work order.")]
    [EndpointDescription("Updates the current state of the specified work order. Only users with the Manager role are authorized.")]
    [EndpointName("UpdateWorkOrderState")]
    public async Task<IActionResult> UpdateState(Guid workOrderId, [FromBody] UpdateWorkOrderStateRequest request, CancellationToken ct)
    {
        var command = new UpdateWorkOrderStateCommand(
            workOrderId,
            request.State);

        var result = await sender.Send(command, ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }

    [HttpPut("{workOrderId:guid}/repair-task")]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRepairTasks(Guid workOrderId, [FromBody] ModifyRepairTaskRequest request, CancellationToken ct)
    {
        var command = new UpdateWorkOrderRepairTasksCommand(workOrderId, request.RepairTaskIds);

        var result = await sender.Send(command, ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }

    [HttpPost("{workOrderId:guid}/cancel")]
    [Authorize(Policy = "SelfScopedWorkOrderAccess")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Mark work order as cancelled.")]
    [EndpointDescription("Cancels the specified work order. Allowed for Managers or the assigned Labor.")]
    [EndpointName("CancelWorkOrder")]
    public async Task<IActionResult> CancelWorkOrder(Guid workOrderId, CancellationToken ct)
    {
        var result = await sender.Send(new CancelWorkOrderCommand(workOrderId), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpGet("schedule")]
    [EndpointSummary("Retrieves the schedule for a given day.")]
    [EndpointDescription("Returns a schedule view for the specified date. If no date is provided, today's schedule is returned. You can optionally filter by labor ID.")]
    [EndpointName("GetDailySchedule")]
    public async Task<ActionResult<ScheduleDto>> GetSchedule(
    [FromQuery] DateOnly? date,
    [FromQuery] Guid? laborId,
    [FromQuery] string? timeZoneId = "UTC",
    CancellationToken ct = default)
    {
        var query = new GetDailyScheduleQuery(timeZoneId, date, 15, laborId);
        var result = await sender.Send(query, ct);

        return result.Match(Ok, Problem);
    }
}