using FluentValidation.TestHelper;

using MechanicShop.Application.Features.Customers.Queries.GetCustomerById;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Customers.Queries.GetCustomerById;

public class GetCustomerByIdQueryValidatorTests
{
    private readonly GetCustomerByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenCustomerIdIsEmpty_ShouldHaveValidationError()
    {
        var query = new GetCustomerByIdQuery(Guid.Empty);

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(request => request.CustomerId)
              .WithErrorCode("CustomerId_Is_Required")
              .WithErrorMessage("CustomerId is required.");
    }

    [Fact]
    public void Validate_WithValidCustomerId_ShouldNotHaveValidationError()
    {
        var query = new GetCustomerByIdQuery(Guid.CreateVersion7());

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(request => request.CustomerId);
    }
}