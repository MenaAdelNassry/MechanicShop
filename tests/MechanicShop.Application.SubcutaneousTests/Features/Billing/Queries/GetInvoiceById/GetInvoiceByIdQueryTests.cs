using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Billing.Queries.GetInvoiceById;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Billing;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Tests.Common.Customers;
using MechanicShop.Tests.Common.Employees;
using MechanicShop.Tests.Common.WorkOrders;

using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Billing.Queries.GetInvoiceById;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetInvoiceByIdQueryTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WhenInvoiceExists_ShouldReturnInvoiceDtoWithFullDeepIncludes()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicle = customer.Vehicles.First();

        var appUserWithLabor = EmployeeFactory.CreateLabor().Value;
        var labor = appUserWithLabor.Employee!;

        var identityResult = await userManager.CreateAsync(appUserWithLabor, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        var workOrder = WorkOrderFactory.CreateTestWorkOrder(
            laborId: labor.Id,
            vehicleId: vehicle.Id,
            startAt: DateTimeOffset.UtcNow.AddHours(-1),
            endAt: DateTimeOffset.UtcNow).Value;

        await context.WorkOrders.AddAsync(workOrder);
        await context.SaveChangesAsync(default);

        var invoiceId = Guid.CreateVersion7();
        var lineItem = InvoiceLineItem.Create(
            invoiceId: invoiceId,
            lineNumber: 1,
            description: "Standard Diagnostic Checkup",
            quantity: 1,
            unitPrice: 150.00m).Value;

        var invoice = Invoice.Create(
            id: invoiceId,
            workOrderId: workOrder.Id,
            items: new List<InvoiceLineItem> { lineItem },
            discountAmount: 10.00m,
            datetime: timeProvider).Value;

        await context.Invoices.AddAsync(invoice);
        await context.SaveChangesAsync(default);

        var query = new GetInvoiceByIdQuery(invoice.Id);

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.InvoiceId.Should().Be(invoice.Id);
        result.Value.WorkOrderId.Should().Be(workOrder.Id);
        result.Value.DiscountAmount.Should().Be(10.00m);

        result.Value.Items.Should().ContainSingle();
        result.Value.Items.First().Description.Should().Be("Standard Diagnostic Checkup");
        result.Value.Items.First().LineTotal.Should().Be(150.00m);
    }

    [Fact]
    public async Task Handle_WhenInvoiceDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var query = new GetInvoiceByIdQuery(Guid.CreateVersion7());

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Type.Should().Be(ErrorKind.NotFound);
        result.TopError.Description.Should().Be("Invoice not found");
    }
}