using FluentValidation.TestHelper;

using MechanicShop.Application.Features.WorkOrders.Commands.UpdateWorkOrderRepairTasks;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.WorkOrders.Commands.UpdateWorkOrderRepairTasks;

public class UpdateWorkOrderRepairTasksCommandValidatorTests
{
    private readonly UpdateWorkOrderRepairTasksCommandValidator _validator;

    public UpdateWorkOrderRepairTasksCommandValidatorTests()
    {
        _validator = new UpdateWorkOrderRepairTasksCommandValidator();
    }

    [Fact]
    public void Validator_WhenWorkOrderIdIsEmpty_ShouldHaveValidationError()
    {
        var command = new UpdateWorkOrderRepairTasksCommand(Guid.Empty, [Guid.CreateVersion7()]);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.WorkOrderId)
              .WithErrorCode("WorkOrderId_Required")
              .WithErrorMessage("WorkOrderId is required.");
    }

    [Fact]
    public void Validator_WhenRepairTaskIdsIsEmpty_ShouldHaveValidationError()
    {
        var command = new UpdateWorkOrderRepairTasksCommand(Guid.CreateVersion7(), []);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RepairTaskIds)
              .WithErrorCode("RepairTasks_Required")
              .WithErrorMessage("At least one repair task must be provided.");
    }

    [Fact]
    public void Validator_WhenCommandIsValid_ShouldNotHaveValidationErrors()
    {
        var command = new UpdateWorkOrderRepairTasksCommand(Guid.CreateVersion7(), [Guid.CreateVersion7()]);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}