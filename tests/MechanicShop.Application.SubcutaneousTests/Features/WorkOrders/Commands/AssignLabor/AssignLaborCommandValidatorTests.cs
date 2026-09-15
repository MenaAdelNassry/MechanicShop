using FluentValidation.TestHelper;

using MechanicShop.Application.Features.WorkOrders.Commands.AssignLabor;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.WorkOrders.Commands.AssignLabor;

public class AssignLaborCommandValidatorTests
{
    private readonly AssignLaborCommandValidator _validator;

    public AssignLaborCommandValidatorTests()
    {
        _validator = new AssignLaborCommandValidator();
    }

    [Fact]
    public void Validator_WhenWorkOrderIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = new AssignLaborCommand(Guid.Empty, Guid.CreateVersion7());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WorkOrderId)
              .WithErrorCode("WorkOrderId_Required")
              .WithErrorMessage("WorkOrderId is required.");
    }

    [Fact]
    public void Validator_WhenLaborIdIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = new AssignLaborCommand(Guid.CreateVersion7(), Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LaborId)
              .WithErrorCode("LaborId_Required")
              .WithErrorMessage("LaborId is required.");
    }

    [Fact]
    public void Validator_WhenCommandIsValid_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var command = new AssignLaborCommand(Guid.CreateVersion7(), Guid.CreateVersion7());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}