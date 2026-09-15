using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Employees.Dtos;
using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Application.Features.Employees.Queries.GetEmployeeById;

public sealed record GetEmployeeByIdQuery(Guid EmployeeId) : ICachedQuery<Result<EmployeeDto>>
{
    public string CacheKey => $"employees:{EmployeeId}";

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);

    public string[] Tags => [$"employee_{EmployeeId}"];
}