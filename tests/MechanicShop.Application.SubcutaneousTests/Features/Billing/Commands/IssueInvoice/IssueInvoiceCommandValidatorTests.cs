using FluentValidation.TestHelper;

using MechanicShop.Application.Features.Billing.Commands.IssueInvoice;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Billing.Commands.IssueInvoice;

public class IssueInvoiceCommandValidatorTests
{
    private readonly IssueInvoiceCommandValidator _validator;

    public IssueInvoiceCommandValidatorTests()
    {
        _validator = new IssueInvoiceCommandValidator();
    }

    [Fact]
    public void Validator_WhenWorkOrderIdIsEmpty_ShouldHaveValidationErrors()
    {
        // Arrange
        var command = new IssueInvoiceCommand(Guid.Empty, null);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkOrderId)
              .WithErrorCode("WorkOrderId_Is_Required")
              .WithErrorMessage("WorkOrderId is required.");

        result.ShouldHaveValidationErrorFor(x => x.WorkOrderId)
              .WithErrorMessage("Invalid Customer Id.");
    }

    [Fact]
    public void Validator_WhenWorkOrderIdIsValid_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var command = new IssueInvoiceCommand(Guid.CreateVersion7(), 10.00m);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}