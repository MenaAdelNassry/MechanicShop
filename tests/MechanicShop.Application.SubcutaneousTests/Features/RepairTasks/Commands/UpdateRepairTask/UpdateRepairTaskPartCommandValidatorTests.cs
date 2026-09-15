using FluentValidation.TestHelper;

using MechanicShop.Application.Features.RepairTasks.Commands.UpdateRepairTask;

using Xunit;

namespace MechanicShop.Application.UnitTests.Features.RepairTasks.Commands.UpdateRepairTask;

public class UpdateRepairTaskPartCommandValidatorTests
{
    private readonly UpdateRepairTaskPartCommandValidator _validator;

    public UpdateRepairTaskPartCommandValidatorTests()
    {
        _validator = new UpdateRepairTaskPartCommandValidator();
    }

    [Fact]
    public void Validator_WhenInventoryItemIdIsEmpty_ShouldHaveValidationError()
    {
        var command = new UpdateRepairTaskPartCommand(Guid.Empty, 2);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.InventoryItemId)
              .WithErrorMessage("InventoryItem ID is required.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validator_WhenQuantityIsLessThanOne_ShouldHaveValidationError(int invalidQuantity)
    {
        var command = new UpdateRepairTaskPartCommand(Guid.CreateVersion7(), invalidQuantity);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Quantity)
              .WithErrorMessage("Quantity must be at least 1.");
    }
}