using FluentValidation.TestHelper;

using MechanicShop.Application.Features.WorkOrders.Commands.UpdateOrderState;
using MechanicShop.Domain.Workorders.Enums;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.WorkOrders.Commands.UpdateOrderState;

public class UpdateWorkOrderStateCommandValidatorTests
{
    private readonly UpdateWorkOrderStateCommandValidator _validator;

    public UpdateWorkOrderStateCommandValidatorTests()
    {
        _validator = new UpdateWorkOrderStateCommandValidator();
    }

    [Fact]
    public void Validator_WhenStateIsInvalidEnum_ShouldHaveValidationError()
    {
        var command = new UpdateWorkOrderStateCommand(Guid.CreateVersion7(), (WorkOrderState)99);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.State)
              .WithErrorCode("WorkOrderStatus_Invalid")
              .WithErrorMessage("Status must be a valid WorkOrderStatus value.");
    }

    [Fact]
    public void Validator_WhenStateIsValidEnum_ShouldNotHaveValidationErrors()
    {
        var command = new UpdateWorkOrderStateCommand(Guid.CreateVersion7(), WorkOrderState.InProgress);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}