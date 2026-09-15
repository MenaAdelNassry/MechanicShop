using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.Commands.CreateWorkOrder;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.RepairTasks.Enums;
using MechanicShop.Domain.Workorders.Enums;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Tests.Common.Customers;
using MechanicShop.Tests.Common.Employees;
using MechanicShop.Tests.Common.RepairTasks;

using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.WorkOrders.Commands.CreateWorkOrder;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CreateWorkOrderCommandHandlerTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WithValidData_ShouldSucceed()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicle = customer.Vehicles.First();
        var repairTask = RepairTaskFactory.CreateRepairTask().Value;

        var appUserWithEmployee = EmployeeFactory.CreateLabor().Value;
        var employee = appUserWithEmployee.Employee!;

        var identityResult = await userManager.CreateAsync(appUserWithEmployee, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.RepairTasks.AddAsync(repairTask);
        await context.SaveChangesAsync(default);

        var scheduledAt = DateTimeOffset.UtcNow.Date
            .AddDays(1)
            .AddHours(10);

        var command = new CreateWorkOrderCommand(
            Spot: Spot.B,
            VehicleId: vehicle.Id,
            StartAt: scheduledAt,
            RepairTaskIds: [repairTask.Id],
            LaborId: employee.Id);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var workOrderDto = result.Value;
        workOrderDto.Vehicle!.VehicleId.Should().Be(vehicle.Id);
        workOrderDto.Labor!.EmployeeId.Should().Be(employee.Id);
        workOrderDto.Spot.Should().Be(Spot.B);
        workOrderDto.RepairTasks.Should().ContainSingle();
        workOrderDto.RepairTasks[0].OriginalRepairTaskId.Should().Be(repairTask.Id);
    }

    [Fact]
    public async Task Handle_WithMissingRepairTask_ShouldFail()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicle = customer.Vehicles.First();

        var appUserWithEmployee = EmployeeFactory.CreateLabor().Value;
        var employee = appUserWithEmployee.Employee!;

        var identityResult = await userManager.CreateAsync(appUserWithEmployee, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.Vehicles.AddAsync(vehicle);
        await context.SaveChangesAsync(default);

        var fakeRepairTaskId = Guid.CreateVersion7();
        var scheduledAt = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(11);

        var command = new CreateWorkOrderCommand(
            Spot: Spot.C,
            VehicleId: vehicle.Id,
            StartAt: scheduledAt,
            RepairTaskIds: [fakeRepairTaskId],
            LaborId: employee.Id);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithOutsideOperatingHours_ShouldFail()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicle = customer.Vehicles.First();
        var repairTask = RepairTaskFactory.CreateRepairTask(repairDurationInMinutes: RepairDurationInMinutes.Min60).Value;

        var appUserWithEmployee = EmployeeFactory.CreateLabor().Value;
        var employee = appUserWithEmployee.Employee!;

        var identityResult = await userManager.CreateAsync(appUserWithEmployee, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.RepairTasks.AddAsync(repairTask);
        await context.SaveChangesAsync(default);

        var scheduledAt = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(4);

        var command = new CreateWorkOrderCommand(Spot.B, vehicle.Id, scheduledAt, [repairTask.Id], employee.Id);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithShortDuration_ShouldFail()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicle = customer.Vehicles.First();
        var repairTask = RepairTaskFactory.CreateRepairTask(repairDurationInMinutes: RepairDurationInMinutes.Min15).Value;

        var appUserWithEmployee = EmployeeFactory.CreateLabor().Value;
        var employee = appUserWithEmployee.Employee!;

        var identityResult = await userManager.CreateAsync(appUserWithEmployee, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.Vehicles.AddAsync(vehicle);
        await context.RepairTasks.AddAsync(repairTask);
        await context.SaveChangesAsync(default);

        var scheduledAt = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(12);

        var command = new CreateWorkOrderCommand(Spot.A, vehicle.Id, scheduledAt, [repairTask.Id], employee.Id);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Code == "WorkOrder_TooShort");
    }

    [Fact]
    public async Task Handle_WithMissingVehicle_ShouldFail()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var repairTask = RepairTaskFactory.CreateRepairTask(repairDurationInMinutes: RepairDurationInMinutes.Min60).Value;

        var appUserWithEmployee = EmployeeFactory.CreateLabor().Value;
        var employee = appUserWithEmployee.Employee!;

        var identityResult = await userManager.CreateAsync(appUserWithEmployee, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.RepairTasks.AddAsync(repairTask);
        await context.SaveChangesAsync(default);

        var scheduledAt = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(13);

        var command = new CreateWorkOrderCommand(Spot.C, Guid.CreateVersion7(), scheduledAt, [repairTask.Id], employee.Id);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithMissingLabor_ShouldFail()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicle = customer.Vehicles.First();
        var repairTask = RepairTaskFactory.CreateRepairTask(repairDurationInMinutes: RepairDurationInMinutes.Min60).Value;

        await context.Customers.AddAsync(customer);
        await context.Vehicles.AddAsync(vehicle);
        await context.RepairTasks.AddAsync(repairTask);
        await context.SaveChangesAsync(default);

        var scheduledAt = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(14);

        var command = new CreateWorkOrderCommand(Spot.C, vehicle.Id, scheduledAt, [repairTask.Id], Guid.CreateVersion7());

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithVehicleConflict_ShouldFail()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicle = customer.Vehicles.First();
        var repairTask = RepairTaskFactory.CreateRepairTask(repairDurationInMinutes: RepairDurationInMinutes.Min60).Value;

        var appUserWithEmployee1 = EmployeeFactory.CreateLabor(firstName: "Mena", lastName: "Adel").Value;
        var appUserWithEmployee2 = EmployeeFactory.CreateLabor(firstName: "John", lastName: "Doe").Value;

        var employee1 = appUserWithEmployee1.Employee!;
        var employee2 = appUserWithEmployee2.Employee!;

        var identityResult1 = await userManager.CreateAsync(appUserWithEmployee1, "SecurePassword123!");
        identityResult1.Succeeded.Should().BeTrue();

        var identityResult2 = await userManager.CreateAsync(appUserWithEmployee2, "SecurePassword123!");
        identityResult2.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.RepairTasks.AddAsync(repairTask);
        await context.SaveChangesAsync(default);

        var scheduledAt = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(15);

        var command1 = new CreateWorkOrderCommand(Spot.C, vehicle.Id, scheduledAt, [repairTask.Id], employee1.Id);
        var command2 = new CreateWorkOrderCommand(Spot.B, vehicle.Id, scheduledAt, [repairTask.Id], employee2.Id);

        // Act
        await mediator.Send(command1);
        var result = await mediator.Send(command2);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Vehicle_Overlapping_WorkOrders");
    }

    [Fact]
    public async Task Handle_WithLaborConflict_ShouldFail()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customer1 = CustomerFactory.CreateCustomer().Value;
        var vehicle1 = customer1.Vehicles.First();
        var customer2 = CustomerFactory.CreateCustomer().Value;
        var vehicle2 = customer2.Vehicles.First();

        var repairTask = RepairTaskFactory.CreateRepairTask(repairDurationInMinutes: RepairDurationInMinutes.Min60).Value;

        var appUserWithEmployee = EmployeeFactory.CreateLabor().Value;
        var employee = appUserWithEmployee.Employee!;

        var identityResult = await userManager.CreateAsync(appUserWithEmployee, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer1);
        await context.Customers.AddAsync(customer2);
        await context.Vehicles.AddAsync(vehicle1);
        await context.Vehicles.AddAsync(vehicle2);
        await context.RepairTasks.AddAsync(repairTask);
        await context.SaveChangesAsync(default);

        var scheduledAt = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(16);

        var command1 = new CreateWorkOrderCommand(Spot.A, vehicle1.Id, scheduledAt, [repairTask.Id], employee.Id);
        var command2 = new CreateWorkOrderCommand(Spot.B, vehicle2.Id, scheduledAt, [repairTask.Id], employee.Id);

        // Act
        await mediator.Send(command1);
        var result = await mediator.Send(command2);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Labor_Occupied");
    }

    [Fact]
    public async Task Handle_WithUnavailableSpot_ShouldFail()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var vehicle1 = VehicleFactory.CreateVehicle().Value;
        var vehicle2 = VehicleFactory.CreateVehicle().Value;
        var customer = CustomerFactory.CreateCustomer(vehicles: [vehicle1, vehicle2]).Value;

        var repairTask = RepairTaskFactory.CreateRepairTask(repairDurationInMinutes: RepairDurationInMinutes.Min60).Value;

        var appUserWithEmployee1 = EmployeeFactory.CreateLabor(firstName: "Mena", lastName: "Adel").Value;
        var appUserWithEmployee2 = EmployeeFactory.CreateLabor(firstName: "John", lastName: "Doe").Value;

        var employee1 = appUserWithEmployee1.Employee!;
        var employee2 = appUserWithEmployee2.Employee!;

        var identityResult1 = await userManager.CreateAsync(appUserWithEmployee1, "SecurePassword123!");
        identityResult1.Succeeded.Should().BeTrue();

        var identityResult2 = await userManager.CreateAsync(appUserWithEmployee2, "SecurePassword123!");
        identityResult2.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.Vehicles.AddAsync(vehicle1);
        await context.Vehicles.AddAsync(vehicle2);
        await context.RepairTasks.AddAsync(repairTask);
        await context.SaveChangesAsync(default);

        var scheduledAt = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(17);

        var command1 = new CreateWorkOrderCommand(Spot.A, vehicle1.Id, scheduledAt, [repairTask.Id], employee1.Id);
        var command2 = new CreateWorkOrderCommand(Spot.A, vehicle2.Id, scheduledAt, [repairTask.Id], employee2.Id);

        // Act
        await mediator.Send(command1);
        var result = await mediator.Send(command2);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("MechanicShop_Spot_Full");
    }
}