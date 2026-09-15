using Asp.Versioning;
using MechanicShop.Api.Requests.Employees;
using MechanicShop.Application.Features.Employees.Commands.CreateEmployee;
using MechanicShop.Application.Features.Employees.Dtos;
using MechanicShop.Application.Features.Employees.Queries.GetEmployeeById;
using MechanicShop.Application.Features.Employees.Queries.GetEmployees;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicShop.Api.Controllers;

[Route("api/v{version:apiVersion}/employees")]
[ApiVersion("1.0")]
[Authorize]
public sealed class EmployeesController(ISender sender) : ApiController
{
    [HttpPost]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request, CancellationToken ct)
    {
        var command = new CreateEmployeeCommand(
        request.FirstName,
        request.LastName,
        request.Email,
        request.PhoneNumber,
        request.Role);

        var result = await sender.Send(command, ct);

        return result.Match(
            id => CreatedAtRoute("GetEmployeeById", new { employeeId = id }, null),
            Problem);
    }

    [HttpGet]
    [Authorize(Policy = "ManagerOnly")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [EndpointSummary("Retrieves a list of all employees.")]
    [EndpointDescription("Returns a collection of registered employees, optionally filtered by their system role.")]
    [EndpointName("GetEmployees")]
    public async Task<ActionResult<List<EmployeeDto>>> Get([FromQuery] string? role, CancellationToken ct)
    {
        var result = await sender.Send(new GetEmployeesQuery(role), ct);

        return result.Match(Ok, Problem);
    }

    [HttpGet("{employeeId:guid}", Name = "GetEmployeeById")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Retrieves an employee by ID.")]
    [EndpointDescription("Returns detailed information about the specified employee if found.")]
    [EndpointName("GetEmployeeById")]
    public async Task<ActionResult<EmployeeDto>> GetById(Guid employeeId, CancellationToken ct)
    {
        var result = await sender.Send(new GetEmployeeByIdQuery(employeeId), ct);

        return result.Match(Ok, Problem);
    }
}
