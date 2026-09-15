using FluentAssertions;

using MechanicShop.Domain.Workorders.Billing;
using MechanicShop.Domain.Workorders.Billing.Enums;
using MechanicShop.Domain.Workorders.Billing.Errors;
using MechanicShop.Tests.Common;
using MechanicShop.Tests.Common.Billing;

using Xunit;

namespace MechanicShop.Domain.UnitTests.WorkOrders.Billing;

public class InvoiceTests
{
    [Fact]
    public void Create_WithValidArgs_ShouldSucceed()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        var workOrderId = Guid.CreateVersion7();
        var items = new List<InvoiceLineItem>
        {
            InvoiceLineItem.Create(Guid.CreateVersion7(), 1, "Oil Change", 2, 50m).Value
        };

        var time = new FakeTimeProvider();
        time.SetUtcNow(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

        // Act
        var result = InvoiceFactory.CreateInvoice(
            id: id,
            workOrderId: workOrderId,
            items: items,
            discount: 10m,
            timeProvider: time);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var invoice = result.Value;
        invoice.Id.Should().Be(id);
        invoice.WorkOrderId.Should().Be(workOrderId);
        invoice.Status.Should().Be(InvoiceStatus.Unpaid);
        invoice.DiscountAmount.Should().Be(10m);
        invoice.Subtotal.Should().Be(100m);

        // Asserting dynamic mathematical behaviors safe from constant value changes
        invoice.TaxAmount.Should().Be(invoice.Subtotal * invoice.TaxRateAtIssuance);
        invoice.Total.Should().Be(invoice.Subtotal - invoice.DiscountAmount + invoice.TaxAmount);
        invoice.IssuedAtUtc.Should().Be(time.GetUtcNow());
    }

    [Fact]
    public void Create_WithEmptyItems_ShouldFail()
    {
        // Arrange
        List<InvoiceLineItem> items = [];

        // Act
        var result = InvoiceFactory.CreateInvoice(items: items);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(InvoiceErrors.LineItemsEmpty.Code);
    }

    [Fact]
    public void ApplyDiscount_WhenUnpaid_ShouldUpdateDiscount()
    {
        // Arrange
        const decimal discount = 15m;
        var invoice = InvoiceFactory.CreateInvoice().Value;
        var originalTotal = invoice.Total;

        // Act
        var result = invoice.ApplyDiscount(discount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invoice.DiscountAmount.Should().Be(discount);
        invoice.Total.Should().Be(originalTotal - discount);
    }

    [Fact]
    public void ApplyDiscount_WithNegativeAmount_ShouldFail()
    {
        // Arrange
        var invoice = InvoiceFactory.CreateInvoice().Value;

        // Act
        var result = invoice.ApplyDiscount(-10m);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(InvoiceErrors.DiscountNegative.Code);
    }

    [Fact]
    public void ApplyDiscount_GreaterThanSubtotal_ShouldFail()
    {
        // Arrange
        var invoice = InvoiceFactory.CreateInvoice().Value;
        var excessiveDiscount = invoice.Subtotal + 1m;

        // Act
        var result = invoice.ApplyDiscount(excessiveDiscount);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(InvoiceErrors.DiscountExceedsSubtotal.Code);
    }

    [Fact]
    public void ApplyDiscount_ValidAmount_ShouldSucceed()
    {
        // Arrange
        var invoice = InvoiceFactory.CreateInvoice().Value;
        const decimal validDiscount = 20m;

        // Act
        var result = invoice.ApplyDiscount(validDiscount);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invoice.DiscountAmount.Should().Be(validDiscount);
    }

    [Fact]
    public void ApplyDiscount_WhenPaid_ShouldFail()
    {
        // Arrange
        var invoice = InvoiceFactory.CreateInvoice().Value;
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(DateTimeOffset.UtcNow);

        invoice.MarkAsPaid(fakeTime).IsSuccess.Should().BeTrue();

        // Act
        var result = invoice.ApplyDiscount(10m);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(InvoiceErrors.InvoiceLocked.Code);
    }

    [Fact]
    public void MarkAsPaid_WhenUnpaid_ShouldSucceed()
    {
        // Arrange
        var invoice = InvoiceFactory.CreateInvoice().Value;
        var time = new FakeTimeProvider();
        time.SetUtcNow(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

        // Act
        var result = invoice.MarkAsPaid(time);

        // Assert
        result.IsSuccess.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.PaidAt.Should().Be(time.GetUtcNow());
    }

    [Fact]
    public void MarkAsPaid_WhenAlreadyPaid_ShouldFail()
    {
        // Arrange
        var invoice = InvoiceFactory.CreateInvoice().Value;
        invoice.MarkAsPaid(TimeProvider.System).IsSuccess.Should().BeTrue();

        // Act
        var result = invoice.MarkAsPaid(TimeProvider.System);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(InvoiceErrors.InvoiceLocked.Code);
    }
}