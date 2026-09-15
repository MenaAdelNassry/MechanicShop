using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.Commands.DeleteWorkOrder;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Tests.Common.Customers;
using MechanicShop.Tests.Common.Employees;
using MechanicShop.Tests.Common.WorkOrders;

using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.WorkOrders.Commands.DeleteWorkOrder;

[Collection(WebAppFactoryCollection.CollectionName)]
public class DeleteWorkOrderCommandTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WithValidRequest_ShouldDeleteWorkOrderSuccessfully()
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

        var workOrder = WorkOrderFactory.CreateTestWorkOrder(vehicleId: vehicle.Id, laborId: labor.Id).Value;

        var identityResult = await userManager.CreateAsync(appUserWithLabor, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        await context.WorkOrders.AddAsync(workOrder);
        await context.SaveChangesAsync(default);

        var command = new DeleteWorkOrderCommand(workOrder.Id);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        using var assertScope = _factory.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var deletedWorkOrder = await assertContext.WorkOrders.FindAsync(workOrder.Id);

        deletedWorkOrder.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenWorkOrderDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new DeleteWorkOrderCommand(Guid.CreateVersion7());

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("ApplicationErrors.WorkOrder.NotFound");
    }
}