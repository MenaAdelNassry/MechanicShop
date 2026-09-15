using FluentAssertions;

using MechanicShop.Application.Features.Billing.Mappers;
using MechanicShop.Domain.Workorders.Billing;
using MechanicShop.Tests.Common.Billing;
using MechanicShop.Tests.Common.Customers;
using MechanicShop.Tests.Common.Employees;
using MechanicShop.Tests.Common.WorkOrders;

using Xunit;

namespace MechanicShop.Application.UnitTests.Mappers;

public class InvoiceMapperTest
{
    [Fact]
    public void ToDto_WithValidInvoiceEntity_ShouldMapCorrectly()
    {
        // Arrange
        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicle = customer.Vehicles.First();
        var labor = EmployeeFactory.CreateLabor().Value.Employee!;

        var workOrder = WorkOrderFactory.CreateTestWorkOrder(
            vehicleId: vehicle.Id,
            laborId: labor.Id).Value;

        workOrder.Vehicle = vehicle;
        workOrder.Labor = labor;
        vehicle.Customer = customer;

        var invoiceLine = InvoiceLineItemFactory.CreateInvoiceLineItem(
            lineNumber: 1,
            description: "Oil Change Service",
            quantity: 1,
            unitPrice: 150m).Value;

        var invoice = InvoiceFactory.CreateInvoice(
            workOrderId: workOrder.Id,
            items: [invoiceLine]).Value;

        invoice.WorkOrder = workOrder;

        // Act
        var dto = invoice.ToDto();

        // Assert
        dto.Should().NotBeNull();
        dto.InvoiceId.Should().Be(invoice.Id);
        dto.WorkOrderId.Should().Be(invoice.WorkOrderId);
        dto.IssuedAtUtc.Should().Be(invoice.IssuedAtUtc);
        dto.Subtotal.Should().Be(invoice.Subtotal);
        dto.TaxAmount.Should().Be(invoice.TaxAmount);
        dto.DiscountAmount.Should().Be(invoice.DiscountAmount);
        dto.Total.Should().Be(invoice.Total);
        dto.PaymentStatus.Should().Be(invoice.Status.ToString());

        dto.Customer.Should().NotBeNull();
        dto.Customer!.CustomerId.Should().Be(customer.Id);
        dto.Customer.Name.Should().Be(customer.Name.FullName);

        dto.Vehicle.Should().NotBeNull();
        dto.Vehicle!.VehicleId.Should().Be(vehicle.Id);
        dto.Vehicle.LicensePlate.Should().Be(vehicle.LicensePlate);

        dto.Items.Should().ContainSingle();
        var itemDto = dto.Items[0];
        itemDto.InvoiceId.Should().Be(invoiceLine.InvoiceId);
        itemDto.LineNumber.Should().Be(invoiceLine.LineNumber);
        itemDto.Description.Should().Be(invoiceLine.Description);
        itemDto.Quantity.Should().Be(invoiceLine.Quantity);
        itemDto.UnitPrice.Should().Be(invoiceLine.UnitPrice);
        itemDto.LineTotal.Should().Be(invoiceLine.LineTotal);
    }

    [Fact]
    public void ToDtos_WithInvoiceEnumerable_ShouldMapListCorrectly()
    {
        // Arrange
        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicle = customer.Vehicles.First();
        var workOrder = WorkOrderFactory.CreateTestWorkOrder(vehicleId: vehicle.Id).Value;
        workOrder.Vehicle = vehicle;
        vehicle.Customer = customer;

        var invoiceLine = InvoiceLineItemFactory.CreateInvoiceLineItem().Value;
        var invoice = InvoiceFactory.CreateInvoice(workOrderId: workOrder.Id, items: [invoiceLine]).Value;
        invoice.WorkOrder = workOrder;

        var entities = new List<Invoice> { invoice };

        // Act
        var dtos = entities.ToDtos();

        // Assert
        dtos.Should().ContainSingle();
        dtos[0].InvoiceId.Should().Be(invoice.Id);
    }

    [Fact]
    public void ToDto_WithNullInvoice_ShouldThrowArgumentNullException()
    {
        // Arrange
        Invoice? invoice = null;

        // Act
        Action act = () => invoice!.ToDto();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}