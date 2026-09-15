using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Employees.Dtos;
using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Application.Features.Employees.Queries.GetEmployees;

public sealed record GetEmployeesQuery(string? Role = null) : ICachedQuery<Result<List<EmployeeDto>>>
{
    public string CacheKey => $"employees:role:{Role?.Trim().ToLower() ?? "all"}";

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);

    public string[] Tags => ["employee_list"];
}