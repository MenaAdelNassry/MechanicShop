using FluentAssertions;

using MechanicShop.Application.Features.Employees.Queries.GetEmployees;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Tests.Common.Employees;

using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Employees.Queries.GetEmployees;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetEmployeesQueryTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WhenFilteredByLaborRole_ShouldReturnOnlyEmployeesWithLaborRole()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var appUserWithLabor = EmployeeFactory.CreateLabor(firstName: "Mena", lastName: "Adel").Value;
        var laborEmployee = appUserWithLabor.Employee!;

        var appUserWithManager = EmployeeFactory.CreateManager(firstName: "Maged", lastName: "Ashref").Value;
        var managerEmployee = appUserWithManager.Employee!;

        var identityResultLabor = await userManager.CreateAsync(appUserWithLabor, "SecurePassword123!");
        identityResultLabor.Succeeded.Should().BeTrue();

        var identityResultManager = await userManager.CreateAsync(appUserWithManager, "SecurePassword123!");
        identityResultManager.Succeeded.Should().BeTrue();

        // Pass "Labor" to test the dynamic filtering logic of the handler
        var query = new GetEmployeesQuery(Role: "Labor");

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var employeeList = result.Value;
        employeeList.Should().NotBeNull();

        employeeList.Should().ContainSingle();
        employeeList.Any(e => e.EmployeeId == managerEmployee.Id).Should().BeFalse();
        employeeList.Any(e => e.EmployeeId == laborEmployee.Id).Should().BeTrue();
        employeeList[0].Role.Should().Be("Labor");
    }

    [Fact]
    public async Task Handle_WhenNoLaborsExist_ShouldReturnEmptyListForLaborFilter()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var appUserWithManager = EmployeeFactory.CreateManager().Value;

        var identityResult = await userManager.CreateAsync(appUserWithManager, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        // Ask for Labors when only a Manager exists in the system
        var query = new GetEmployeesQuery(Role: "Labor");

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}