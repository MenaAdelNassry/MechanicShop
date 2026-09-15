using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.Commands.UpdateWorkOrderRepairTasks;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.Workorders;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Tests.Common.Customers;
using MechanicShop.Tests.Common.Employees;
using MechanicShop.Tests.Common.RepairTasks;
using MechanicShop.Tests.Common.WorkOrders;

using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.WorkOrders.Commands.UpdateWorkOrderRepairTasks;

[Collection(WebAppFactoryCollection.CollectionName)]
public class UpdateWorkOrderRepairTasksCommandTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WithValidRequest_ShouldUpdateRepairTasksSuccessfully()
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

        var newTask = RepairTaskFactory.CreateRepairTask().Value;

        var startAt = DateTime.UtcNow.Date.AddHours(9).AddMinutes(30);
        var endAt = startAt.AddHours(1);
        var workOrder = WorkOrderFactory.CreateTestWorkOrder(
            vehicleId: vehicle.Id,
            laborId: labor.Id,
            startAt: startAt,
            endAt: endAt,
            spot: Domain.Workorders.Enums.Spot.A).Value;

        var identityResult = await userManager.CreateAsync(appUserWithLabor, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.RepairTasks.AddAsync(newTask);
        await context.SaveChangesAsync(default);

        await context.WorkOrders.AddAsync(workOrder);
        await context.SaveChangesAsync(default);

        var command = new UpdateWorkOrderRepairTasksCommand(workOrder.Id, [newTask.Id]);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        using var assertScope = _factory.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var updatedWorkOrder = await assertContext.WorkOrders
            .Include(w => w.RepairTasks)
            .FirstOrDefaultAsync(w => w.Id == workOrder.Id);

        updatedWorkOrder.Should().NotBeNull();
        updatedWorkOrder!.RepairTasks.Should().ContainSingle();
        updatedWorkOrder.RepairTasks.First().OriginalRepairTaskId.Should().Be(newTask.Id);
    }

    [Fact]
    public async Task Handle_WhenWorkOrderDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new UpdateWorkOrderRepairTasksCommand(Guid.CreateVersion7(), [Guid.CreateVersion7()]);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("ApplicationErrors.WorkOrder.NotFound");
    }

    [Fact]
    public async Task Handle_WhenSomeRepairTasksDoNotExist_ShouldReturnNotFoundError()
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
        await context.SaveChangesAsync(default);

        var workOrder = WorkOrderFactory.CreateTestWorkOrder(vehicleId: vehicle.Id, laborId: employee.Id).Value;
        await context.WorkOrders.AddAsync(workOrder);
        await context.SaveChangesAsync(default);

        var fakeTaskId = Guid.CreateVersion7();
        var command = new UpdateWorkOrderRepairTasksCommand(workOrder.Id, [fakeTaskId]);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("RepairTask.NotFound");
    }

    [Fact]
    public async Task Handle_WhenNewTasksCauseLaborOccupancyConflict_ShouldReturnConflictError()
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
        await context.SaveChangesAsync(default);

        var shortTask = RepairTaskFactory.CreateRepairTask(repairDurationInMinutes: Domain.RepairTasks.Enums.RepairDurationInMinutes.Min60).Value;
        var longTask = RepairTaskFactory.CreateRepairTask(repairDurationInMinutes: Domain.RepairTasks.Enums.RepairDurationInMinutes.Min120).Value;

        var startAt = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(10);
        var endAt = startAt.AddHours(1);

        var scheduledTask = WorkOrderTaskFactory.CreateTask(
            originalTaskId: shortTask.Id,
            name: shortTask.Name,
            laborCost: shortTask.LaborCost,
            estimatedDurationInMins: shortTask.EstimatedDurationInMins,
            parts: new List<WorkOrderTaskPart>());

        var workOrder1 = WorkOrderFactory.CreateTestWorkOrder(
            id: Guid.CreateVersion7(),
            vehicleId: vehicle.Id,
            startAt: startAt,
            endAt: endAt,
            laborId: employee.Id,
            spot: Domain.Workorders.Enums.Spot.A,
            repairTasks: new List<WorkOrderTask> { scheduledTask }).Value;

        var workOrder2 = WorkOrderFactory.CreateTestWorkOrder(
            id: Guid.CreateVersion7(),
            vehicleId: vehicle.Id,
            startAt: startAt.AddHours(1.5),
            endAt: endAt.AddHours(1.5),
            laborId: employee.Id,
            spot: Domain.Workorders.Enums.Spot.B,
            repairTasks: new List<WorkOrderTask> { scheduledTask }).Value;

        await context.RepairTasks.AddRangeAsync(shortTask, longTask);
        await context.SaveChangesAsync(default);

        await context.WorkOrders.AddRangeAsync(workOrder1, workOrder2);
        await context.SaveChangesAsync(default);

        var command = new UpdateWorkOrderRepairTasksCommand(workOrder1.Id, [longTask.Id]);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("Employee.LaborOccupied");
    }
}