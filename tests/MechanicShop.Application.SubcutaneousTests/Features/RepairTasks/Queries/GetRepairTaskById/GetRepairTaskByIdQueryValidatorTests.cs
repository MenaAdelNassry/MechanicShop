using FluentValidation.TestHelper;

using MechanicShop.Application.Features.RepairTasks.Queries.GetRepairTaskById;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.RepairTasks.Queries.GetRepairTaskById;

public class GetRepairTaskByIdQueryValidatorTests
{
    private readonly GetRepairTaskByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenRepairTaskIdIsEmpty_ShouldHaveValidationError()
    {
        // 1?? Arrange
        var query = new GetRepairTaskByIdQuery(Guid.Empty);

        // 2?? Act
        var result = _validator.TestValidate(query);

        // 3?? Assert
        result.ShouldHaveValidationErrorFor(request => request.RepairTaskId)
              .WithErrorCode("RepairTaskId_Is_Required")
              .WithErrorMessage("RepairTaskId is required.");
    }

    [Fact]
    public void Validate_WithValidRepairTaskId_ShouldNotHaveValidationError()
    {
        // 1?? Arrange
        var query = new GetRepairTaskByIdQuery(Guid.CreateVersion7());

        // 2?? Act
        var result = _validator.TestValidate(query);

        // 3?? Assert
        result.ShouldNotHaveValidationErrorFor(request => request.RepairTaskId);
    }
}