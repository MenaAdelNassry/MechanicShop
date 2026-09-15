using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.RepairTasks.Commands.RemoveRepairTask;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.Workorders;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Tests.Common.Customers;
using MechanicShop.Tests.Common.Employees;
using MechanicShop.Tests.Common.RepairTasks;
using MechanicShop.Tests.Common.WorkOrders;

using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.RepairTasks.Commands.RemoveRepairTask;

[Collection(WebAppFactoryCollection.CollectionName)]
public class RemoveRepairTaskCommandTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WithValidIdAndNotUsed_ShouldDeleteSuccessfully()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var repairTask = RepairTaskFactory.CreateRepairTask().Value;
        await context.RepairTasks.AddAsync(repairTask);
        await context.SaveChangesAsync(default);

        var command = new RemoveRepairTaskCommand(repairTask.Id);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        using var assertScope = _factory.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var deletedTask = await assertContext.RepairTasks.FindAsync(repairTask.Id);
        deletedTask.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenRepairTaskDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new RemoveRepairTaskCommand(Guid.CreateVersion7());

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("RepairTask.NotFound");
    }

    [Fact]
    public async Task Handle_WhenRepairTaskIsInUseByWorkOrder_ShouldReturnInUseConflictError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customer = CustomerFactory.CreateCustomer().Value;
        var appUserWithLabor = EmployeeFactory.CreateLabor().Value;
        var labor = appUserWithLabor.Employee!;
        var vehicle = customer.Vehicles.First();
        var repairTask = RepairTaskFactory.CreateRepairTask().Value;

        var workOrderTask = WorkOrderTaskFactory.CreateTask(
            originalTaskId: repairTask.Id,
            name: repairTask.Name,
            laborCost: repairTask.LaborCost,
            estimatedDurationInMins: repairTask.EstimatedDurationInMins,
            parts: []);

        var workOrder = WorkOrderFactory.CreateTestWorkOrder(
            vehicleId: vehicle.Id,
            laborId: labor.Id,
            repairTasks: [workOrderTask]).Value;

        var identityResult = await userManager.CreateAsync(appUserWithLabor, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.RepairTasks.AddAsync(repairTask);
        await context.SaveChangesAsync(default);

        await context.WorkOrders.AddAsync(workOrder);
        await context.SaveChangesAsync(default);

        var command = new RemoveRepairTaskCommand(repairTask.Id);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("RepairTask.InUse");
    }
}