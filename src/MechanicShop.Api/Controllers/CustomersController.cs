using Asp.Versioning;

using MechanicShop.Api.Requests.Customers;
using MechanicShop.Application.Common.Models;
using MechanicShop.Application.Features.Customers.Commands.CreateCustomer;
using MechanicShop.Application.Features.Customers.Commands.RemoveCustomer;
using MechanicShop.Application.Features.Customers.Commands.UpdateCustomer;
using MechanicShop.Application.Features.Customers.Dtos;
using MechanicShop.Application.Features.Customers.Dtos.History;
using MechanicShop.Application.Features.Customers.Queries.GetCustomerById;
using MechanicShop.Application.Features.Customers.Queries.GetCustomerHistory;
using MechanicShop.Application.Features.Customers.Queries.GetCustomers;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicShop.Api.Controllers;

[Route("api/v{version:apiVersion}/customers")]
[ApiVersion("1.0")]
[Authorize]
public sealed class CustomersController(ISender sender) : ApiController
{
    [HttpGet]
    [EndpointSummary("Retrieves a list of customers.")]
    [EndpointDescription("Returns all customers associated with the current user.")]
    [EndpointName("GetCustomers")]
    public async Task<ActionResult<PaginatedList<CustomerDto>>> Get(
        [FromQuery] PageRequest pageRequest,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var query = new GetCustomersQuery(pageRequest.Page, pageRequest.PageSize, searchTerm);
        var result = await sender.Send(query, ct);

        return result.Match(Ok, Problem);
    }

    [HttpGet("{customerId:guid}", Name = "GetCustomerById")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieves a customer by ID.")]
    [EndpointDescription("Returns detailed information about the specified customer if found.")]
    [EndpointName("GetCustomerById")]
    public async Task<ActionResult<CustomerDto>> GetById(Guid customerId, CancellationToken ct)
    {
        var result = await sender.Send(new GetCustomerByIdQuery(customerId), ct);

        return result.Match(Ok, Problem);
    }

    [HttpGet("{customerId:guid}/history")]
    [Authorize(Policy = "ManagerOnly")]
    [EndpointSummary("Gets comprehensive service history and lifetime statistics for a customer.")]
    [EndpointDescription("Returns the complete timeline of work orders, registered vehicles, total visits, and lifetime expenditure for the specified customer.")]
    [EndpointName("GetCustomerHistory")]
    public async Task<ActionResult<CustomerHistoryDto>> GetCustomerHistory([FromRoute] Guid customerId, CancellationToken ct)
    {
        var result = await sender.Send(new GetCustomerHistoryQuery(customerId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Creates a new customer.")]
    [EndpointDescription("Adds a new customer to the system.")]
    [EndpointName("CreateCustomer")]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var vehicles = request.Vehicles
            .ConvertAll(v => new CreateVehicleCommand(v.Make, v.Model, v.Year, v.LicensePlate));

        var result = await sender.Send(
            new CreateCustomerCommand(
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.Email,
                vehicles),
            ct);

        return result.Match(
            response => CreatedAtRoute(
                routeName: "GetCustomerById",
                routeValues: new { customerId = response.CustomerId },
                value: response),
            Problem);
    }

    [HttpPut("{customerId:guid}")]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Updates an existing customer.")]
    [EndpointDescription("Updates a customer and its associated vehicle.")]
    [EndpointName("UpdateCustomer")]
    public async Task<IActionResult> Update(Guid customerId, [FromBody] UpdateCustomerRequest request, CancellationToken ct)
    {
        var vehicles = request.Vehicles
            .ConvertAll(v => new UpdateVehicleCommand(v.VehicleId, v.Make, v.Model, v.Year, v.LicensePlate));

        var command = new UpdateCustomerCommand(
            customerId,
            request.FirstName,
            request.LastName,
            request.PhoneNumber,
            request.Email,
            vehicles);

        var result = await sender.Send(command, ct);

        return result.Match(_ => NoContent(), Problem);
    }

    [HttpDelete("{customerId:guid}")]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Removes a customer.")]
    [EndpointDescription("Deletes the specified customer from the system.")]
    [EndpointName("RemoveCustomer")]
    public async Task<IActionResult> Delete(Guid customerId, CancellationToken ct)
    {
        var result = await sender.Send(new RemoveCustomerCommand(customerId), ct);

        return result.Match(_ => NoContent(), Problem);
    }
}