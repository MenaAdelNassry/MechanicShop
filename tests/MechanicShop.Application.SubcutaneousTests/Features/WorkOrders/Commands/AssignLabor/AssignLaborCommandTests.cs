using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.Commands.AssignLabor;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Tests.Common.Customers;
using MechanicShop.Tests.Common.Employees;
using MechanicShop.Tests.Common.WorkOrders;

using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.WorkOrders.Commands.AssignLabor;

[Collection(WebAppFactoryCollection.CollectionName)]
public class AssignLaborCommandTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WithValidRequest_ShouldAssignLaborSuccessfully()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicle = customer.Vehicles.First();

        var appUserWithLabor = EmployeeFactory.CreateLabor().Value;
        var appUserWithLabor2 = EmployeeFactory.CreateLabor().Value;

        var labor = appUserWithLabor.Employee!;
        var labor2 = appUserWithLabor2.Employee!;

        var workOrder = WorkOrderFactory.CreateTestWorkOrder(
            vehicleId: vehicle.Id,
            laborId: labor.Id).Value;

        var identityResultLabor1 = await userManager.CreateAsync(appUserWithLabor, "SecurePassword123!");
        identityResultLabor1.Succeeded.Should().BeTrue();

        var identityResultLabor2 = await userManager.CreateAsync(appUserWithLabor2, "SecurePassword123!");
        identityResultLabor2.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        await context.WorkOrders.AddAsync(workOrder);
        await context.SaveChangesAsync(default);

        var command = new AssignLaborCommand(workOrder.Id, labor2.Id);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        using var assertScope = _factory.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var updatedWorkOrder = await assertContext.WorkOrders.FindAsync(workOrder.Id);

        updatedWorkOrder.Should().NotBeNull();
        updatedWorkOrder!.LaborId.Should().Be(labor2.Id);
    }

    [Fact]
    public async Task Handle_WhenWorkOrderDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var appUserWithEmployee = EmployeeFactory.CreateLabor().Value;
        var employee = appUserWithEmployee.Employee!;

        var identityResult = await userManager.CreateAsync(appUserWithEmployee, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        var command = new AssignLaborCommand(Guid.CreateVersion7(), employee.Id);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("ApplicationErrors.WorkOrder.NotFound");
    }

    [Fact]
    public async Task Handle_WhenLaborDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicle = customer.Vehicles.First();

        var appUserWithLabor = EmployeeFactory.CreateLabor().Value;
        var labor = appUserWithLabor.Employee!;

        var workOrder = WorkOrderFactory.CreateTestWorkOrder(vehicleId: vehicle.Id, laborId: labor.Id).Value;

        var identityResult = await userManager.CreateAsync(appUserWithLabor, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        await context.WorkOrders.AddAsync(workOrder);
        await context.SaveChangesAsync(default);

        var command = new AssignLaborCommand(workOrder.Id, Guid.CreateVersion7());

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Employee.LaborNotFound");
    }

    [Fact]
    public async Task Handle_WhenLaborIsOccupied_ShouldReturnConflictError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customerId = Guid.CreateVersion7();
        var vehicle1 = VehicleFactory.CreateVehicle(customerId: customerId).Value;
        var vehicle2 = VehicleFactory.CreateVehicle(customerId: customerId).Value;
        var customer = CustomerFactory.CreateCustomer(vehicles: [vehicle1, vehicle2], id: customerId).Value;

        var appUserWithLabor = EmployeeFactory.CreateLabor().Value;
        var labor = appUserWithLabor.Employee!;

        var startAt = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(10);
        var endAt = startAt.AddHours(2);

        var conflictingWorkOrder = WorkOrderFactory.CreateTestWorkOrder(
            id: Guid.CreateVersion7(),
            vehicleId: vehicle1.Id,
            startAt: startAt,
            endAt: endAt,
            laborId: labor.Id).Value;

        var targetWorkOrder = WorkOrderFactory.CreateTestWorkOrder(
            id: Guid.CreateVersion7(),
            vehicleId: vehicle2.Id,
            startAt: startAt.AddMinutes(30),
            endAt: endAt.AddMinutes(30),
            laborId: labor.Id).Value;

        var identityResult = await userManager.CreateAsync(appUserWithLabor, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        await context.WorkOrders.AddRangeAsync(conflictingWorkOrder, targetWorkOrder);
        await context.SaveChangesAsync(default);

        var command = new AssignLaborCommand(targetWorkOrder.Id, labor.Id);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Employee.LaborOccupied");
    }
}