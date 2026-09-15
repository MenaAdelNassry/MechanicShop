using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.Workorders.Enums;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Tests.Common.Customers;
using MechanicShop.Tests.Common.Employees;
using MechanicShop.Tests.Common.RepairTasks;
using MechanicShop.Tests.Common.WorkOrders;

using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

using QueryType = MechanicShop.Application.Features.WorkOrders.Queries.GetWorkOrderByIdQuery.GetWorkOrderByIdQuery;

namespace MechanicShop.Application.SubcutaneousTests.Features.WorkOrders.Queries.GetWorkOrderByIdQuery;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetWorkOrderByIdQueryTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WhenWorkOrderExists_ShouldReturnWorkOrderWithAllRelationships()
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
        await context.Vehicles.AddAsync(vehicle);
        await context.RepairTasks.AddAsync(repairTask);
        await context.SaveChangesAsync(default);

        var workOrderTask = WorkOrderTaskFactory.CreateTask(
            originalTaskId: repairTask.Id,
            name: repairTask.Name,
            laborCost: repairTask.LaborCost,
            estimatedDurationInMins: repairTask.EstimatedDurationInMins,
            parts: []);

        var workOrder = WorkOrderFactory.CreateTestWorkOrder(
            vehicleId: vehicle.Id,
            laborId: employee.Id,
            spot: Spot.A,
            repairTasks: [workOrderTask]).Value;

        await context.WorkOrders.AddAsync(workOrder);
        await context.SaveChangesAsync(default);

        var query = new QueryType(workOrder.Id);

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var dto = result.Value;
        dto.Should().NotBeNull();
        dto.WorkOrderId.Should().Be(workOrder.Id);
        dto.Spot.Should().Be(Spot.A);

        dto.Vehicle.Should().NotBeNull();
        dto.Vehicle!.VehicleId.Should().Be(vehicle.Id);

        dto.Labor.Should().NotBeNull();
        dto.Labor!.EmployeeId.Should().Be(employee.Id);

        dto.RepairTasks.Should().ContainSingle();
        dto.RepairTasks.First().Id.Should().Be(workOrderTask.Id);
    }

    [Fact]
    public async Task Handle_WhenWorkOrderDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var query = new QueryType(Guid.CreateVersion7());

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("ApplicationErrors.WorkOrder.NotFound");
    }
}