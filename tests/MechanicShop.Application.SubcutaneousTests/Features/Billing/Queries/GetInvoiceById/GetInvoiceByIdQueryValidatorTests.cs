using FluentValidation.TestHelper;

using MechanicShop.Application.Features.Billing.Queries.GetInvoiceById;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Billing.Queries.GetInvoiceById;

public class GetInvoiceByIdQueryValidatorTests
{
    private readonly GetInvoiceByIdQueryValidator _validator;

    public GetInvoiceByIdQueryValidatorTests()
    {
        _validator = new GetInvoiceByIdQueryValidator();
    }

    [Fact]
    public void Validator_WhenInvoiceIdIsEmpty_ShouldHaveValidationErrors()
    {
        // Arrange
        var query = new GetInvoiceByIdQuery(Guid.Empty);

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
        var query = new GetInvoiceByIdQuery(Guid.CreateVersion7());

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}