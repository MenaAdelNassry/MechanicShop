using MechanicShop.Application.Features.Employees.Dtos;
using MechanicShop.Domain.Employees;

namespace MechanicShop.Application.Features.Employees.Mappers;

public static class EmployeeMapper
{
    public static EmployeeDto ToDto(this Employee employee, string email = "")
    {
        return new EmployeeDto
        {
            EmployeeId = employee.Id,
            Name = employee.Name.FullName,
            PhoneNumber = employee.PhoneNumber.Value,
            Role = employee.Role.ToString(),
            Email = email
        };
    }

    public static List<EmployeeDto> ToDtos(this IEnumerable<Employee> entities)
    {
        return entities.Select(e => e.ToDto()).ToList();
    }
}