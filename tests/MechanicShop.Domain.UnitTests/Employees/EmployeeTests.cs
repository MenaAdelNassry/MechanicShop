using FluentAssertions;

using MechanicShop.Domain.Common.ValueObjects;
using MechanicShop.Domain.Employees;
using MechanicShop.Domain.Identity;
using MechanicShop.Tests.Common.Employees;

using Xunit;

namespace MechanicShop.Domain.UnitTests.Employees;

public class EmployeeTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        const string firstName = "John";
        const string lastName = "Doe";
        const Role role = Role.Labor;

        // Act
        var result = EmployeeFactory.CreateEmployee(employeeId: id, firstName: firstName, lastName: lastName, role: role);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var employee = result.Value.Employee!;
        employee.Id.Should().Be(id);
        employee.Name.FirstName.Should().Be(firstName);
        employee.Name.LastName.Should().Be(lastName);
        employee.Role.Should().Be(role);
        employee.Name.FullName.Should().Be($"{firstName} {lastName}");
    }

    [Fact]
    public void Create_WithEmptyId_ShouldFail()
    {
        // Act
        var result = EmployeeFactory.CreateEmployee(Guid.Empty, "John", "Doe", Role.Manager, "test@gmail.com");

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(EmployeeErrors.IdRequired.Code);
        result.TopError.Description.Should().Be(EmployeeErrors.IdRequired.Description);
    }

    [Fact]
    public void Create_WithEmptyFirstName_ShouldFail()
    {
        // Act & Assert
        var personNameResult = PersonName.Create(" ", "Doe");

        personNameResult.IsError.Should().BeTrue();

        personNameResult.TopError.Code.Should().Be("Name.FirstNameRequired");
    }

    [Fact]
    public void Create_WithEmptyLastName_ShouldFail()
    {
        // Act & Assert
        var personNameResult = PersonName.Create("John", " ");

        personNameResult.IsError.Should().BeTrue();
        personNameResult.TopError.Code.Should().Be("Name.LastNameRequired");
    }

    [Fact]
    public void Create_WithInvalidRole_ShouldFail()
    {
        // Act
        var result = EmployeeFactory.CreateEmployee(Guid.CreateVersion7(), "John", "Doe", (Role)999);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(EmployeeErrors.RoleInvalid.Code);
        result.TopError.Description.Should().Be(EmployeeErrors.RoleInvalid.Description);
    }
}