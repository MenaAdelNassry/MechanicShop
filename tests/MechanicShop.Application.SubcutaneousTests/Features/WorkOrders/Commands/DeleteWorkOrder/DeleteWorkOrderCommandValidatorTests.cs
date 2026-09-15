using FluentValidation.TestHelper;

using MechanicShop.Application.Features.WorkOrders.Commands.DeleteWorkOrder;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.WorkOrders.Commands.DeleteWorkOrder;

public class DeleteWorkOrderCommandValidatorTests
{
    private readonly DeleteWorkOrderCommandValidator _validator;

    public DeleteWorkOrderCommandValidatorTests()
    {
        _validator = new DeleteWorkOrderCommandValidator();
    }

    [Fact]
    public void Validator_WhenWorkOrderIdIsEmpty_ShouldHaveValidationError()
    {
        var command = new DeleteWorkOrderCommand(Guid.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.WorkOrderId)
              .WithErrorCode("WorkOrderId_Required")
              .WithErrorMessage("WorkOrderId is required.");
    }

    [Fact]
    public void Validator_WhenWorkOrderIdIsValid_ShouldNotHaveValidationErrors()
    {
        var command = new DeleteWorkOrderCommand(Guid.CreateVersion7());

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}