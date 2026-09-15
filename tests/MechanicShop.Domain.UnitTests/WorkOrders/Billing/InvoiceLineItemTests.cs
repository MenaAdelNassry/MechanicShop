using FluentAssertions;

using MechanicShop.Domain.Workorders.Billing;
using MechanicShop.Domain.Workorders.Billing.Errors;

using Xunit;

namespace MechanicShop.Domain.UnitTests.WorkOrders.Billing;

public class InvoiceLineItemTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var invoiceId = Guid.CreateVersion7();
        const int lineNumber = 1;
        const string description = "Brake Pad";
        const int quantity = 2;
        const decimal unitPrice = 50m;

        // Act
        var result = InvoiceLineItem.Create(invoiceId, lineNumber, description, quantity, unitPrice);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var item = result.Value;
        item.InvoiceId.Should().Be(invoiceId);
        item.LineNumber.Should().Be(lineNumber);
        item.Description.Should().Be(description);
        item.Quantity.Should().Be(quantity);
        item.UnitPrice.Should().Be(unitPrice);
        item.LineTotal.Should().Be(100m);
    }

    [Fact]
    public void Create_WithEmptyInvoiceId_ShouldFail()
    {
        // Act
        var result = InvoiceLineItem.Create(Guid.Empty, 1, "Item", 1, 10m);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(InvoiceLineItemErrors.InvoiceIdRequired.Code);
        result.TopError.Description.Should().Be(InvoiceLineItemErrors.InvoiceIdRequired.Description);
    }

    [Fact]
    public void Create_WithInvalidLineNumber_ShouldFail()
    {
        // Act
        var result = InvoiceLineItem.Create(Guid.CreateVersion7(), 0, "Item", 1, 10m);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(InvoiceLineItemErrors.LineNumberInvalid.Code);
        result.TopError.Description.Should().Be(InvoiceLineItemErrors.LineNumberInvalid.Description);
    }

    [Fact]
    public void Create_WithEmptyDescription_ShouldFail()
    {
        // Act
        var result = InvoiceLineItem.Create(Guid.CreateVersion7(), 1, " ", 1, 10m);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(InvoiceLineItemErrors.DescriptionRequired.Code);
        result.TopError.Description.Should().Be(InvoiceLineItemErrors.DescriptionRequired.Description);
    }

    [Fact]
    public void Create_WithInvalidQuantity_ShouldFail()
    {
        // Act
        var result = InvoiceLineItem.Create(Guid.CreateVersion7(), 1, "Item", 0, 10m);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(InvoiceLineItemErrors.QuantityInvalid.Code);
        result.TopError.Description.Should().Be(InvoiceLineItemErrors.QuantityInvalid.Description);
    }

    [Fact]
    public void Create_WithInvalidUnitPrice_ShouldFail()
    {
        // Act
        var result = InvoiceLineItem.Create(Guid.CreateVersion7(), 1, "Item", 1, 0m);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(InvoiceLineItemErrors.UnitPriceInvalid.Code);
        result.TopError.Description.Should().Be(InvoiceLineItemErrors.UnitPriceInvalid.Description);
    }
}