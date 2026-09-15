using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Employees.Dtos;
using MechanicShop.Application.Features.Employees.Mappers;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Identity;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Employees.Queries.GetEmployees;

public sealed class GetEmployeesQueryHandler(IAppDbContext context)
    : IRequestHandler<GetEmployeesQuery, Result<List<EmployeeDto>>>
{
    public async Task<Result<List<EmployeeDto>>> Handle(GetEmployeesQuery query, CancellationToken ct)
    {
        var dbQuery = context.Employees.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Role) && Enum.TryParse<Role>(query.Role, true, out var roleEnum))
        {
            dbQuery = dbQuery.Where(e => e.Role == roleEnum);
        }

        var employees = await dbQuery
            .OrderBy(e => e.Name.LastName)
            .ThenBy(e => e.Name.FirstName)
            .ToListAsync(ct);

        return employees.ToDtos();
    }
}