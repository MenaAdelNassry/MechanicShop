using FluentValidation.TestHelper;

using MechanicShop.Application.Features.Identity.Queries.GenerateTokens;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Identity.Queries.GenerateTokens;

public class GenerateTokenQueryValidatorTests
{
    private readonly GenerateTokenQueryValidator _validator;

    public GenerateTokenQueryValidatorTests()
    {
        _validator = new GenerateTokenQueryValidator();
    }

    [Fact]
    public void Validator_WhenEmailIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var query = new GenerateTokenQuery(string.Empty, "SecurePassword123");

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
              .WithErrorCode("Email_Null_Or_Empty")
              .WithErrorMessage("Email cannot be null or empty");
    }

    [Fact]
    public void Validator_WhenPasswordIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var query = new GenerateTokenQuery("user@example.com", string.Empty);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
              .WithErrorCode("Password_Null_Or_Empty")
              .WithErrorMessage("Password cannot be null or empty.");
    }

    [Fact]
    public void Validator_WhenQueryIsValid_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var query = new GenerateTokenQuery("user@example.com", "SecurePassword123");

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}