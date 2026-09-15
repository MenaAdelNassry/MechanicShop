using FluentValidation.TestHelper;

using MechanicShop.Application.Features.Dashboard.Queries.GetWorkOrderStats;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.Dashboard.Queries.GetWorkOrderStats;

public class GetWorkOrderStatsQueryValidatorTests
{
    private readonly GetWorkOrderStatsQueryValidator _validator;

    public GetWorkOrderStatsQueryValidatorTests()
    {
        _validator = new GetWorkOrderStatsQueryValidator();
    }

    [Fact]
    public void Validator_WhenDateIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var query = new GetWorkOrderStatsQuery(default);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Date)
              .WithErrorCode("Date_Is_Required")
              .WithErrorMessage("Date is required.");
    }

    [Fact]
    public void Validator_WhenDateIsValid_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var query = new GetWorkOrderStatsQuery(DateOnly.FromDateTime(DateTime.UtcNow));

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}