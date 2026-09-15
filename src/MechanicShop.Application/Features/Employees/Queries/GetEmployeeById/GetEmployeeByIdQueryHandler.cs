using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Employees.Dtos;
using MechanicShop.Application.Features.Employees.Mappers;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Employees;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.Employees.Queries.GetEmployeeById;

public sealed class GetEmployeeByIdQueryHandler(
    ILogger<GetEmployeeByIdQueryHandler> logger,
    IAppDbContext context
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

        return employee.ToDto();
    }
}