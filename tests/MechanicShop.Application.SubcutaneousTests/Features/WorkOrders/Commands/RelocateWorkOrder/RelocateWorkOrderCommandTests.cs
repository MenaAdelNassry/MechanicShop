using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.Commands.RelocateWorkOrder;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.Workorders.Enums;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Tests.Common.Customers;
using MechanicShop.Tests.Common.Employees;
using MechanicShop.Tests.Common.WorkOrders;

using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.WorkOrders.Commands.RelocateWorkOrder;

[Collection(WebAppFactoryCollection.CollectionName)]
public class RelocateWorkOrderCommandTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WithValidRequest_ShouldRelocateSuccessfully()
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

        var originalStart = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(10);
        var originalEnd = originalStart.AddHours(2);

        var workOrder = WorkOrderFactory.CreateTestWorkOrder(
            vehicleId: vehicle.Id,
            laborId: labor.Id,
            startAt: originalStart,
            endAt: originalEnd,
            spot: Spot.A).Value;

        var identityResult = await userManager.CreateAsync(appUserWithLabor, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        await context.WorkOrders.AddAsync(workOrder);
        await context.SaveChangesAsync(default);

        var newStart = originalStart.AddHours(4);
        var command = new RelocateWorkOrderCommand(workOrder.Id, newStart, Spot.B);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        using var assertScope = _factory.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var updatedWorkOrder = await assertContext.WorkOrders.FindAsync(workOrder.Id);

        updatedWorkOrder.Should().NotBeNull();
        updatedWorkOrder!.StartAtUtc.Should().Be(newStart);
        updatedWorkOrder.Spot.Should().Be(Spot.B);
    }

    [Fact]
    public async Task Handle_WhenWorkOrderDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new RelocateWorkOrderCommand(Guid.CreateVersion7(), DateTimeOffset.UtcNow.AddDays(1), Spot.A);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("ApplicationErrors.WorkOrder.NotFound");
    }

    [Fact]
    public async Task Handle_WhenSpotIsUnavailable_ShouldReturnConflictError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var vehicle1 = VehicleFactory.CreateVehicle().Value;
        var vehicle2 = VehicleFactory.CreateVehicle().Value;
        var customer = CustomerFactory.CreateCustomer(vehicles: [vehicle1, vehicle2]).Value;

        var appUserWithLabor = EmployeeFactory.CreateLabor().Value;
        var labor = appUserWithLabor.Employee!;

        var startAt = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(10);
        var endAt = startAt.AddHours(2);

        var conflictingWorkOrder = WorkOrderFactory.CreateTestWorkOrder(
            id: Guid.CreateVersion7(),
            vehicleId: vehicle1.Id,
            laborId: labor.Id,
            startAt: startAt,
            endAt: endAt,
            spot: Spot.A).Value;

        var targetWorkOrder = WorkOrderFactory.CreateTestWorkOrder(
            id: Guid.CreateVersion7(),
            vehicleId: vehicle2.Id,
            laborId: labor.Id,
            startAt: startAt.AddDays(1),
            endAt: endAt.AddDays(1),
            spot: Spot.B).Value;

        var identityResult = await userManager.CreateAsync(appUserWithLabor, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        await context.WorkOrders.AddRangeAsync(conflictingWorkOrder, targetWorkOrder);
        await context.SaveChangesAsync(default);

        var command = new RelocateWorkOrderCommand(targetWorkOrder.Id, startAt, Spot.A);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("MechanicShop_Spot_Full");
    }

    [Fact]
    public async Task Handle_WhenLaborIsOccupied_ShouldReturnConflictError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var vehicle1 = VehicleFactory.CreateVehicle().Value;
        var vehicle2 = VehicleFactory.CreateVehicle().Value;
        var customer = CustomerFactory.CreateCustomer(vehicles: [vehicle1, vehicle2]).Value;

        var appUserWithEmployee = EmployeeFactory.CreateLabor().Value;
        var employee = appUserWithEmployee.Employee!;

        var startAt = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(10);
        var endAt = startAt.AddHours(2);

        var existingWorkOrder = WorkOrderFactory.CreateTestWorkOrder(
            id: Guid.CreateVersion7(),
            vehicleId: vehicle1.Id,
            startAt: startAt,
            endAt: endAt,
            laborId: employee.Id).Value;

        var targetWorkOrder = WorkOrderFactory.CreateTestWorkOrder(
            id: Guid.CreateVersion7(),
            vehicleId: vehicle2.Id,
            startAt: startAt.AddDays(1),
            endAt: endAt.AddDays(1),
            laborId: employee.Id).Value;

        var identityResult = await userManager.CreateAsync(appUserWithEmployee, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        await context.WorkOrders.AddRangeAsync(existingWorkOrder, targetWorkOrder);
        await context.SaveChangesAsync(default);

        var command = new RelocateWorkOrderCommand(targetWorkOrder.Id, startAt, Spot.B);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Employee.LaborOccupied");
    }

    [Fact]
    public async Task Handle_WhenVehicleIsAlreadyScheduled_ShouldReturnConflictError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicle = customer.Vehicles.First();

        var appUserWithLabor1 = EmployeeFactory.CreateLabor(firstName: "Mena", lastName: "Adel").Value;
        var appUserWithLabor2 = EmployeeFactory.CreateLabor(firstName: "John", lastName: "Doe").Value;

        var labor = appUserWithLabor1.Employee!;
        var labor2 = appUserWithLabor2.Employee!;

        var startAt = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(10);
        var endAt = startAt.AddHours(2);

        var existingWorkOrder = WorkOrderFactory.CreateTestWorkOrder(
            id: Guid.CreateVersion7(),
            vehicleId: vehicle.Id,
            laborId: labor.Id,
            startAt: startAt,
            endAt: endAt,
            spot: Spot.A).Value;

        var targetWorkOrder = WorkOrderFactory.CreateTestWorkOrder(
            id: Guid.CreateVersion7(),
            vehicleId: vehicle.Id,
            laborId: labor2.Id,
            startAt: startAt.AddDays(1),
            endAt: endAt.AddDays(1),
            spot: Spot.B).Value;

        var identityResult1 = await userManager.CreateAsync(appUserWithLabor1, "SecurePassword123!");
        identityResult1.Succeeded.Should().BeTrue();

        var identityResult2 = await userManager.CreateAsync(appUserWithLabor2, "SecurePassword123!");
        identityResult2.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        await context.WorkOrders.AddRangeAsync(existingWorkOrder, targetWorkOrder);
        await context.SaveChangesAsync(default);

        var command = new RelocateWorkOrderCommand(targetWorkOrder.Id, startAt, Spot.B);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Vehicle_Overlapping_WorkOrder");
    }
}