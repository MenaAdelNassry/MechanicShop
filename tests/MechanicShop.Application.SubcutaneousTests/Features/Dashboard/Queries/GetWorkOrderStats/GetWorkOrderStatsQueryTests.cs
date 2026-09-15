using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Dashboard.Queries.GetWorkOrderStats;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.Workorders;
using MechanicShop.Domain.Workorders.Billing;
using MechanicShop.Domain.Workorders.Enums;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Tests.Common.Customers;
using MechanicShop.Tests.Common.Employees;
using MechanicShop.Tests.Common.WorkOrders;

using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Dashboard.Queries.GetWorkOrderStats;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetWorkOrderStatsQueryTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WhenNoWorkOrdersExistForDate_ShouldReturnEmptyStatsWithTargetDate()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var targetDate = new DateOnly(2029, 7, 16);
        var query = new GetWorkOrderStatsQuery(targetDate);

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Date.Should().Be(targetDate);
        result.Value.Total.Should().Be(0);
        result.Value.NetProfit.Should().Be(0);
        result.Value.CompletionRate.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenWorkOrdersExist_ShouldCalculateAccurateAggregationsAndFinancialMetrics()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var targetDate = new DateOnly(2026, 7, 16);
        var query = new GetWorkOrderStatsQuery(targetDate);

        var startUtc = new DateTimeOffset(new DateTime(2026, 7, 16, 10, 0, 0, DateTimeKind.Utc));
        var endUtc = startUtc.AddHours(2);

        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicle = customer.Vehicles.First();

        var appUserWithLabor = EmployeeFactory.CreateLabor().Value;
        var labor = appUserWithLabor.Employee!;

        var identityResult = await userManager.CreateAsync(appUserWithLabor, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        var samplePart = new WorkOrderTaskPart(Guid.CreateVersion7(), "Oil Filter", 150.00m, 1);
        var sampleTask = WorkOrderTaskFactory.CreateTask(
            name: "Oil Change Service",
            laborCost: 100.00m,
            estimatedDurationInMins: Domain.RepairTasks.Enums.RepairDurationInMinutes.Min30,
            parts: new List<WorkOrderTaskPart> { samplePart });

        var completedOrder = WorkOrderFactory.CreateTestWorkOrder(
            vehicleId: vehicle.Id,
            startAt: startUtc,
            endAt: endUtc,
            laborId: labor.Id,
            repairTasks: new List<WorkOrderTask> { sampleTask }).Value;

        await context.WorkOrders.AddAsync(completedOrder);
        await context.SaveChangesAsync(default);

        completedOrder.UpdateState(WorkOrderState.InProgress, timeProvider);
        completedOrder.UpdateState(WorkOrderState.Completed, timeProvider);

        var invoiceId = Guid.CreateVersion7();
        var lineItem = InvoiceLineItem.Create(
            invoiceId: invoiceId,
            lineNumber: 1,
            description: "Full Engine Diagnostic and Repair",
            quantity: 1,
            unitPrice: 500.00m).Value;

        var invoice = Invoice.Create(
            id: invoiceId,
            workOrderId: completedOrder.Id,
            items: new List<InvoiceLineItem> { lineItem },
            discountAmount: 0.00m,
            datetime: timeProvider).Value;

        completedOrder.Invoice = invoice;

        var scheduledTask = WorkOrderTaskFactory.CreateTask(
            name: "Simple Inspection",
            laborCost: 0.00m,
            estimatedDurationInMins: Domain.RepairTasks.Enums.RepairDurationInMinutes.Min15,
            parts: new List<WorkOrderTaskPart>());

        var scheduledOrder = WorkOrderFactory.CreateTestWorkOrder(
            vehicleId: vehicle.Id,
            startAt: startUtc.AddHours(1),
            endAt: endUtc.AddHours(1),
            laborId: labor.Id,
            repairTasks: new List<WorkOrderTask> { scheduledTask }).Value;

        var outOfRangeOrder = WorkOrderFactory.CreateTestWorkOrder(
            vehicleId: vehicle.Id,
            laborId: labor.Id,
            startAt: startUtc.AddDays(1),
            endAt: endUtc.AddDays(1)).Value;

        await context.Invoices.AddAsync(invoice);
        await context.WorkOrders.AddRangeAsync(scheduledOrder, outOfRangeOrder);
        await context.SaveChangesAsync(default);

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var stats = result.Value;
        stats.Should().NotBeNull();
        stats.Date.Should().Be(targetDate);
        stats.Total.Should().Be(2);
        stats.Completed.Should().Be(1);
        stats.Scheduled.Should().Be(1);

        stats.TotalLaborCost.Should().Be(100.00m);
        stats.TotalPartsCost.Should().Be(150.00m);

        stats.UniqueVehicles.Should().Be(1);
        stats.UniqueCustomers.Should().Be(1);
    }
}