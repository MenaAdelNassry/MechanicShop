using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Billing.Commands.IssueInvoice;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.Workorders;
using MechanicShop.Domain.Workorders.Billing;
using MechanicShop.Domain.Workorders.Billing.Enums;
using MechanicShop.Domain.Workorders.Enums;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Tests.Common.Customers;
using MechanicShop.Tests.Common.Employees;
using MechanicShop.Tests.Common.WorkOrders;

using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Billing.Commands.IssueInvoice;

[Collection(WebAppFactoryCollection.CollectionName)]
public class IssueInvoiceCommandTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WithValidCompletedWorkOrder_ShouldIssueInvoiceSuccessfully()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicle = customer.Vehicles.First();
        var appUserWithEmployee = EmployeeFactory.CreateLabor().Value;
        var labor = appUserWithEmployee.Employee!;

        var identityResult = await userManager.CreateAsync(appUserWithEmployee, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        // Setting up a task with real parts
        var samplePart = WorkOrderTaskPartFactory.CreatePart(Guid.CreateVersion7(), "Brake Pads", 100.00m, 2); // 200.00m
        var sampleTask = WorkOrderTaskFactory.CreateTask(
            name: "Brake Replacement Service",
            laborCost: 150.00m,
            estimatedDurationInMins: Domain.RepairTasks.Enums.RepairDurationInMinutes.Min30,
            parts: new List<WorkOrderTaskPart> { samplePart });

        var startUtc = DateTimeOffset.UtcNow.AddMinutes(-60);
        var endUtc = DateTimeOffset.UtcNow;

        var workOrder = WorkOrderFactory.CreateTestWorkOrder(
            laborId: labor.Id,
            vehicleId: vehicle.Id,
            startAt: startUtc,
            endAt: endUtc,
            repairTasks: new List<WorkOrderTask> { sampleTask }).Value;

        await context.WorkOrders.AddAsync(workOrder);
        await context.SaveChangesAsync(default);

        // Execute proper State Transitions to allow invoice generation
        workOrder.UpdateState(WorkOrderState.InProgress, timeProvider);
        workOrder.UpdateState(WorkOrderState.Completed, timeProvider);
        await context.SaveChangesAsync(default);

        var command = new IssueInvoiceCommand(workOrder.Id, 20.00m); // $20.00 Discount

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.WorkOrderId.Should().Be(workOrder.Id);
        result.Value.DiscountAmount.Should().Be(20.00m);

        // Isolate assertion scope to query database storage directly
        using var assertScope = _factory.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var dbInvoice = await assertContext.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == result.Value.InvoiceId);

        dbInvoice.Should().NotBeNull();
        dbInvoice!.WorkOrderId.Should().Be(workOrder.Id);
        dbInvoice.DiscountAmount.Should().Be(20.00m);
        dbInvoice.Status.Should().Be(InvoiceStatus.Unpaid);
        dbInvoice.LineItems.Should().HaveCount(2);
        dbInvoice.LineItems.First().Description.Should().Contain("Brake Replacement Service");
    }

    [Fact]
    public async Task Handle_WhenWorkOrderIsNotCompleted_ShouldReturnWorkOrderMustBeCompletedError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        // Ensure FK targets exist (Customer/Vehicle and Employee) and supply their ids to the WorkOrder factory.
        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicle = customer.Vehicles.First();
        var appUserWithEmployee = EmployeeFactory.CreateLabor().Value;
        var labor = appUserWithEmployee.Employee!;

        var identityResult = await userManager.CreateAsync(appUserWithEmployee, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        var workOrder = WorkOrderFactory.CreateTestWorkOrder(
            vehicleId: vehicle.Id,
            laborId: labor.Id).Value; // Left in default 'Scheduled' state

        await context.WorkOrders.AddAsync(workOrder);
        await context.SaveChangesAsync(default);

        var command = new IssueInvoiceCommand(workOrder.Id, 0m);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("WorkOrder.InvoiceIssuance.InvalidState");
        result.TopError.Description.Should().Be("WorkOrder must be in 'Completed' state to issue an invoice.");
    }

    [Fact]
    public async Task Handle_WhenInvoiceAlreadyIssued_ShouldReturnInvoiceAlreadyIssuedError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        // Make sure FK targets exist and create a WorkOrder using those persisted ids.
        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicle = customer.Vehicles.First();
        var appUserWithEmployee = EmployeeFactory.CreateLabor().Value;
        var labor = appUserWithEmployee.Employee!;

        var identityResult = await userManager.CreateAsync(appUserWithEmployee, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        var workOrder = WorkOrderFactory.CreateTestWorkOrder(
            vehicleId: vehicle.Id,
            laborId: labor.Id).Value;

        await context.WorkOrders.AddAsync(workOrder);
        await context.SaveChangesAsync(default);

        // Advance to Completed state
        workOrder.UpdateState(WorkOrderState.InProgress, timeProvider);
        workOrder.UpdateState(WorkOrderState.Completed, timeProvider);
        await context.SaveChangesAsync(default);

        // Manually seed an initial invoice to provoke the constraint violation error
        var lineItem = InvoiceLineItem.Create(Guid.CreateVersion7(), 1, "Initial Run", 1, 50.00m).Value;
        var existingInvoice = Invoice.Create(Guid.CreateVersion7(), workOrder.Id, new List<InvoiceLineItem> { lineItem }, 0m, timeProvider).Value;

        await context.Invoices.AddAsync(existingInvoice);
        await context.SaveChangesAsync(default);

        var command = new IssueInvoiceCommand(workOrder.Id, 0m);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("ApplicationErrors.Invoice.AlreadyIssuedForWorkOrder");
    }

    [Fact]
    public async Task Handle_WhenWorkOrderDoesNotExist_ShouldReturnWorkOrderNotFoundError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new IssueInvoiceCommand(Guid.CreateVersion7(), 0m);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be("WorkOrderErrors.WorkOrderNotFound");
    }
}