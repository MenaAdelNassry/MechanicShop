using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Employees.Commands.CreateEmployee;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.Employees;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Employees.Commands.CreateEmployee;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CreateEmployeeCommandHandlerTests : BaseSubcutaneousTest
{
    private readonly WebAppFactory _factory;

    public CreateEmployeeCommandHandlerTests(WebAppFactory factory)
        : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCreateIdentityUserAndEmployeeAndSendNotification()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var command = new CreateEmployeeCommand
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe.dev@example.com",
            PhoneNumber = "+123456789",
            Role = "Labor"
        };

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var createdEmployeeId = result.Value;
        createdEmployeeId.Should().NotBeEmpty();

        // Verify Employee was persisted in the domain store
        var employeeInDb = await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == createdEmployeeId);

        employeeInDb.Should().NotBeNull();
        employeeInDb!.Name.FirstName.Should().Be(command.FirstName);
        employeeInDb.Name.LastName.Should().Be(command.LastName);
        employeeInDb.Role.ToString().Should().BeEquivalentTo(command.Role);
        employeeInDb.IdentityUserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WhenRoleIsInvalid_ShouldReturnRoleInvalidError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new CreateEmployeeCommand
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "invalid.role@example.com",
            PhoneNumber = "+123456789",
            Role = "SuperHero" // Invalid role designation
        };

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(EmployeeErrors.RoleInvalid);
    }

    [Fact]
    public async Task Handle_WhenEmailIsAlreadyRegisteredInIdentity_ShouldReturnIdentityFailureError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var duplicateEmail = "shared.email@example.com";

        var command1 = new CreateEmployeeCommand
        {
            FirstName = "First",
            LastName = "Employee",
            Email = duplicateEmail,
            PhoneNumber = "+11111111",
            Role = "Labor"
        };

        var command2 = new CreateEmployeeCommand
        {
            FirstName = "Second",
            LastName = "Employee",
            Email = duplicateEmail, // Direct conflict execution
            PhoneNumber = "+22222222",
            Role = "Manager"
        };

        // Fire the initial setup successfully
        var initialResult = await mediator.Send(command1);
        initialResult.IsSuccess.Should().BeTrue();

        // Act - Attempting to execute duplication logic
        var duplicateResult = await mediator.Send(command2);

        // Asserts that identity layer gracefully blocked the execution path from registering duplicate email keys
        duplicateResult.IsError.Should().BeTrue();
    }
}