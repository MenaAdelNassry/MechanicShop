using FluentAssertions;

using MassTransit;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.Consumers;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.Workorders.Events;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Tests.Common.Customers;
using MechanicShop.Tests.Common.Employees;
using MechanicShop.Tests.Common.WorkOrders;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.WorkOrders.EventHandlers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class SendWorkOrderCompletedEmailHandlerTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Consume_WhenWorkOrderExists_ShouldSendOrderCompletedEmailNotificationToCustomer()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SendWorkOrderCompletedEmailConsumer>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var mockNotificationService = Substitute.For<INotificationService>();

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

        var handler = new SendWorkOrderCompletedEmailConsumer(mockNotificationService, context, logger);
        var notification = new WorkOrderCompleted(default) { WorkOrderId = workOrder.Id };

        var consumeContext = Substitute.For<ConsumeContext<WorkOrderCompleted>>();
        consumeContext.Message.Returns(notification);
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        // Act
        await handler.Consume(consumeContext);

        // Assert
        await mockNotificationService
            .Received(1)
            .SendOrderCompletedNotificationAsync(customer.Email.Value, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_WhenWorkOrderDoesNotExist_ShouldNotSendOrderCompletedNotification()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SendWorkOrderCompletedEmailConsumer>>();

        var mockNotificationService = Substitute.For<INotificationService>();

        var handler = new SendWorkOrderCompletedEmailConsumer(mockNotificationService, context, logger);
        var notification = new WorkOrderCompleted(default) { WorkOrderId = Guid.CreateVersion7() };

        var consumeContext = Substitute.For<ConsumeContext<WorkOrderCompleted>>();
        consumeContext.Message.Returns(notification);
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        // Act
        await handler.Consume(consumeContext);

        // Assert
        await mockNotificationService
            .DidNotReceive()
            .SendOrderCompletedNotificationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}