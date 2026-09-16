using Asp.Versioning;

using MechanicShop.Api.Requests.Spots;
using MechanicShop.Application.Features.Spots.Commands.CreateSpot;
using MechanicShop.Application.Features.Spots.Commands.ToggleSpotActive;
using MechanicShop.Application.Features.Spots.Commands.UpdateSpot;
using MechanicShop.Application.Features.Spots.Dtos;
using MechanicShop.Application.Features.Spots.Queries.GetActiveSpots;
using MechanicShop.Application.Features.Spots.Queries.GetAllSpots;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicShop.Api.Controllers;

[Route("api/v{version:apiVersion}/spots")]
[ApiVersion("1.0")]
[Authorize]
public sealed class SpotsController(ISender sender) : ApiController
{
    [HttpGet]
    [EndpointSummary("Retrieves all active service bays (for booking/scheduling).")]
    public async Task<ActionResult<IReadOnlyList<SpotDto>>> GetActive(CancellationToken ct)
    {
        var result = await sender.Send(new GetActiveSpotsQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("all")]
    [Authorize(Policy = "ManagerOnly")]
    [EndpointSummary("Retrieves all service bays (active and inactive) for management.")]
    public async Task<ActionResult<IReadOnlyList<SpotDto>>> GetAll(CancellationToken ct)
    {
        var result = await sender.Send(new GetAllSpotsQuery(), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost]
    [Authorize(Policy = "ManagerOnly")]
    [EndpointSummary("Creates a new service bay.")]
    public async Task<IActionResult> Create([FromBody] CreateSpotRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new CreateSpotCommand(request.Name, request.Description), ct);
        return result.Match(bay => Ok(bay), Problem);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManagerOnly")]
    [EndpointSummary("Updates an existing service bay.")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSpotRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new UpdateSpotCommand(id, request.Name, request.Description), ct);
        return result.Match(_ => NoContent(), Problem);
    }

    [HttpPatch("{id:guid}/toggle-active")]
    [Authorize(Policy = "ManagerOnly")]
    [EndpointSummary("Toggles the active state of a service bay.")]
    public async Task<IActionResult> ToggleActive(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ToggleSpotActiveCommand(id), ct);
        return result.Match(_ => NoContent(), Problem);
    }
}