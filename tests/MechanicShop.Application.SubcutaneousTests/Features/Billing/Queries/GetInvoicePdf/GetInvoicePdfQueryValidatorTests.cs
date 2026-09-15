using FluentValidation.TestHelper;

using MechanicShop.Application.Features.Billing.Queries.GetInvoicePdf;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Billing.Queries.GetInvoicePdf;

public class GetInvoicePdfQueryValidatorTests
{
    private readonly GetInvoicePdfQueryValidator _validator;

    public GetInvoicePdfQueryValidatorTests()
    {
        _validator = new GetInvoicePdfQueryValidator();
    }

    [Fact]
    public void Validator_WhenInvoiceIdIsEmpty_ShouldHaveValidationErrors()
    {
        // Arrange
        var query = new GetInvoicePdfQuery(Guid.Empty);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InvoiceId)
              .WithErrorCode("InvoiceId_Is_Required")
              .WithErrorMessage("InvoiceId is required.");

        result.ShouldHaveValidationErrorFor(x => x.InvoiceId)
              .WithErrorMessage("Invalid Invoice Id.");
    }

    [Fact]
    public void Validator_WhenInvoiceIdIsValid_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var query = new GetInvoicePdfQuery(Guid.CreateVersion7());

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}