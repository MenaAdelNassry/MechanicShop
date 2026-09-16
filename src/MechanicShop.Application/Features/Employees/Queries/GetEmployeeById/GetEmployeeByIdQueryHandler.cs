using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Employees.Dtos;
using MechanicShop.Application.Features.Employees.Mappers;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Employees.Queries.GetEmployeeById;

public sealed class GetEmployeeByIdQueryHandler(
    ILogger<GetEmployeeByIdQueryHandler> logger,
    IAppDbContext context,
    IIdentityService identityService
) : IRequestHandler<GetEmployeeByIdQuery, Result<EmployeeDto>>
{
    public async Task<Result<EmployeeDto>> Handle(GetEmployeeByIdQuery query, CancellationToken ct)
    {
        var employee = await context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == query.EmployeeId, ct);

        if (employee is null)
        {
            logger.LogWarning("Employee with id {EmployeeId} was not found", query.EmployeeId);
            return ApplicationErrors.Employees.NotFound;
        }

        string email = string.Empty;
        if (employee.IdentityUserId != Guid.Empty)
        {
            var userResult = await identityService.GetUserByIdAsync(employee.IdentityUserId.ToString());
            if (!userResult.IsError)
            {
                email = userResult.Value.Email;
            }
        }

        return employee.ToDto(email);
    }
}