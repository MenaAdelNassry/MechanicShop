using FluentValidation.TestHelper;

using MechanicShop.Application.Features.Customers.Commands.RemoveCustomer;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Customers.Commands.RemoveCustomer;

public class RemoveCustomerCommandValidatorTests
{
    private readonly RemoveCustomerCommandValidator _validator;

    public RemoveCustomerCommandValidatorTests()
    {
        _validator = new RemoveCustomerCommandValidator();
    }

    [Fact]
    public void Validator_WhenCustomerIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = new RemoveCustomerCommand(Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId)
              .WithErrorMessage("Customer Id is required.");
    }

    [Fact]
    public void Validator_WhenCustomerIdIsValid_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var command = new RemoveCustomerCommand(Guid.CreateVersion7());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}