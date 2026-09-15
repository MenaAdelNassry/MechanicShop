using FluentValidation.TestHelper;

using MechanicShop.Application.Features.RepairTasks.Commands.RemoveRepairTask;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.RepairTasks.Commands.RemoveRepairTask;

public class RemoveRepairTaskCommandValidatorTests
{
    private readonly RemoveRepairTaskCommandValidator _validator;

    public RemoveRepairTaskCommandValidatorTests()
    {
        _validator = new RemoveRepairTaskCommandValidator();
    }

    [Fact]
    public void Validator_WhenRepairTaskIdIsEmpty_ShouldHaveValidationError()
    {
        var command = new RemoveRepairTaskCommand(Guid.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RepairTaskId)
              .WithErrorMessage("Repair task Id is required.");
    }

    [Fact]
    public void Validator_WhenRepairTaskIdIsValid_ShouldNotHaveValidationErrors()
    {
        var command = new RemoveRepairTaskCommand(Guid.CreateVersion7());

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}