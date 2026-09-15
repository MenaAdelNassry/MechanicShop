using Asp.Versioning;
using MechanicShop.Api.Requests.Setup;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Common.ValueObjects;
using MechanicShop.Domain.Employees;
using MechanicShop.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MechanicShop.Api.Controllers;

[Route("api/v{version:apiVersion}/setup")]
[ApiVersion("1.0")]
public sealed class SetupController : ApiController
{
    private readonly IIdentityService _identityService;
    private readonly IAppDbContext _context;

    public SetupController(IIdentityService identityService, IAppDbContext context)
    {
        _identityService = identityService;
        _context = context;
    }

    [HttpPost("initial-manager")]
    [AllowAnonymous]
    public async Task<IActionResult> InitialManager([FromBody] InitialManagerRequest request, CancellationToken ct)
    {
        // Check if any manager exists
        var anyManager = await _identityService.AnyUserInRoleAsync(nameof(Role.Manager));

        if (anyManager)
        {
            return Problem(new List<Error> { Error.Conflict("Setup.AlreadyInitialized", "System already initialized") });
        }

        var nameResult = PersonName.Create(request.FirstName, request.LastName);
        if (nameResult.IsError)
        {
            return Problem(nameResult.Errors);
        }

        var phoneResult = PhoneNumber.Create(request.PhoneNumber);
        if (phoneResult.IsError)
        {
            return Problem(phoneResult.Errors);
        }

        var userId = Guid.CreateVersion7();

        var createUserResult = await _identityService.CreateUserAsync(userId, nameResult.Value.FullName, request.Email, request.Password, nameof(Role.Manager), emailConfirmed: true, ct: ct);

        if (createUserResult.IsError)
        {
            return Problem(createUserResult.Errors);
        }

        var employeeId = Guid.CreateVersion7();

        var employeeResult = Employee.Create(employeeId, nameResult.Value, phoneResult.Value, Role.Manager, userId);

        if (employeeResult.IsError)
        {
            return Problem(employeeResult.Errors);
        }

        _context.Employees.Add(employeeResult.Value);

        await _context.SaveChangesAsync(ct);

        return Created(string.Empty, null);
    }
}