using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Billing.Queries.GetInvoicePdf;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Billing;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Tests.Common.Customers;
using MechanicShop.Tests.Common.Employees;
using MechanicShop.Tests.Common.WorkOrders;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Billing.Queries.GetInvoicePdf;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetInvoicePdfQueryTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;
    private readonly IInvoicePdfGenerator _mockPdfGenerator = Substitute.For<IInvoicePdfGenerator>();
    private readonly ILogger<GetInvoicePdfQueryHandler> _mockLogger = Substitute.For<ILogger<GetInvoicePdfQueryHandler>>();

    [Fact]
    public async Task Handle_WhenInvoiceExists_ShouldGeneratePdfSuccessfully()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var invoiceId = Guid.CreateVersion7();
        var workOrderId = Guid.CreateVersion7();

        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicle = customer.Vehicles.First();

        var appUserWithLabor = EmployeeFactory.CreateLabor().Value;
        var labor = appUserWithLabor.Employee!;

        var lineItem = InvoiceLineItem.Create(invoiceId, 1, "Clutch Repair Service", 1, 350.00m).Value;
        var invoice = Invoice.Create(invoiceId, workOrderId, new List<InvoiceLineItem> { lineItem }, 0m, timeProvider).Value;

        var identityResult = await userManager.CreateAsync(appUserWithLabor, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        var workOrder = WorkOrderFactory.CreateTestWorkOrder(
            id: workOrderId,
            vehicleId: vehicle.Id,
            laborId: labor.Id).Value;

        await context.WorkOrders.AddAsync(workOrder);
        await context.Invoices.AddAsync(invoice);
        await context.SaveChangesAsync(default);

        var expectedBytes = new byte[] { 1, 2, 3, 4, 5 };
        _mockPdfGenerator.Generate(Arg.Is<Invoice>(i => i.Id == invoiceId)).Returns(expectedBytes);

        var handler = new GetInvoicePdfQueryHandler(_mockLogger, _mockPdfGenerator, context);
        var query = new GetInvoicePdfQuery(invoiceId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Content.Should().Equal(expectedBytes);
        result.Value.FileName.Should().Be($"invoice-{invoiceId}.pdf");
    }

    [Fact]
    public async Task Handle_WhenInvoiceDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var query = new GetInvoicePdfQuery(Guid.CreateVersion7());
        var handler = new GetInvoicePdfQueryHandler(_mockLogger, _mockPdfGenerator, context);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Type.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_WhenGeneratorThrowsException_ShouldCatchExceptionAndReturnFailureError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var invoiceId = Guid.CreateVersion7();
        var workOrderId = Guid.CreateVersion7();

        var lineItem = InvoiceLineItem.Create(invoiceId, 1, "Engine Swap", 1, 1200.00m).Value;
        var invoice = Invoice.Create(invoiceId, workOrderId, new List<InvoiceLineItem> { lineItem }, 0m, timeProvider).Value;

        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicle = customer.Vehicles.First();

        var appUserWithLabor = EmployeeFactory.CreateLabor().Value;
        var labor = appUserWithLabor.Employee!;

        var identityResult = await userManager.CreateAsync(appUserWithLabor, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        var workOrder = WorkOrderFactory.CreateTestWorkOrder(
            id: workOrderId,
            vehicleId: vehicle.Id,
            laborId: labor.Id).Value;

        await context.WorkOrders.AddAsync(workOrder);
        await context.Invoices.AddAsync(invoice);
        await context.SaveChangesAsync(default);

        _mockPdfGenerator.Generate(Arg.Any<Invoice>())
            .Returns(_ => throw new InvalidOperationException("Font engine crash."));

        var handler = new GetInvoicePdfQueryHandler(_mockLogger, _mockPdfGenerator, context);
        var query = new GetInvoicePdfQuery(invoiceId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Type.Should().Be(ErrorKind.Failure);
        result.TopError.Code.Should().Be("InvoicePdfGenerationError");
        result.TopError.Description.Should().Be("An error occurred while generating the invoice PDF.");
    }
}