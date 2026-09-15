using FluentAssertions;
using MechanicShop.Application.Features.Employees.Mappers;
using MechanicShop.Domain.Employees;
using MechanicShop.Tests.Common.Employees;
using Xunit;

namespace MechanicShop.Application.UnitTests.Mappers;

public class EmployeeMapperTest
{
    [Fact]
    public void ToDto_WithValidEmployeeEntity_ShouldMapCorrectly()
    {
        // Arrange
        // Create an employee with any role (e.g., Manager or Labor depending on your factory implementation)
        var employee = EmployeeFactory.CreateEmployee().Value.Employee!;

        // Act
        var dto = employee.ToDto();

        // Assert
        dto.Should().NotBeNull();
        dto.EmployeeId.Should().Be(employee.Id);
        dto.Name.Should().Be(employee.Name.FullName);
        dto.Role.Should().Be(employee.Role.ToString());
    }

    [Fact]
    public void ToDtos_WithEmployeeEnumerable_ShouldMapListCorrectly()
    {
        // Arrange
        var employee = EmployeeFactory.CreateEmployee().Value.Employee!;
        var entities = new List<Employee> { employee };

        // Act
        var dtos = entities.ToDtos();

        // Assert
        dtos.Should().ContainSingle();
        dtos[0].EmployeeId.Should().Be(employee.Id);
        dtos[0].Name.Should().Be(employee.Name.FullName);
        dtos[0].Role.Should().Be(employee.Role.ToString());
    }
}