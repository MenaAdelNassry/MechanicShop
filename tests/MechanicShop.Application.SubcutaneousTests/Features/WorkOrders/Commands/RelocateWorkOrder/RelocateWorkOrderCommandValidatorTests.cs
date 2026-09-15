using FluentValidation.TestHelper;

using MechanicShop.Application.Features.WorkOrders.Commands.RelocateWorkOrder;
using MechanicShop.Domain.Workorders.Enums;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.WorkOrders.Commands.RelocateWorkOrder;

public class RelocateWorkOrderCommandValidatorTests
{
    private readonly RescheduleAppointmentCommandValidator _validator;

    public RelocateWorkOrderCommandValidatorTests()
    {
        _validator = new RescheduleAppointmentCommandValidator();
    }

    [Fact]
    public void Validator_WhenWorkOrderIdIsEmpty_ShouldHaveValidationError()
    {
        var command = new RelocateWorkOrderCommand(
            WorkOrderId: Guid.Empty,
            NewStartAt: DateTimeOffset.UtcNow.AddDays(1),
            NewSpot: Spot.A);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.WorkOrderId);
    }

    [Fact]
    public void Validator_WhenNewStartAtIsInThePast_ShouldHaveValidationError()
    {
        var command = new RelocateWorkOrderCommand(
            WorkOrderId: Guid.CreateVersion7(),
            NewStartAt: DateTimeOffset.UtcNow.AddHours(-1),
            NewSpot: Spot.A);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.NewStartAt)
              .WithErrorMessage("New start time must be in the future.");
    }

    [Fact]
    public void Validator_WhenNewSpotIsInvalidEnum_ShouldHaveValidationError()
    {
        var command = new RelocateWorkOrderCommand(
            WorkOrderId: Guid.CreateVersion7(),
            NewStartAt: DateTimeOffset.UtcNow.AddDays(1),
            NewSpot: (Spot)99);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.NewSpot);
    }

    [Fact]
    public void Validator_WhenCommandIsValid_ShouldNotHaveValidationErrors()
    {
        var command = new RelocateWorkOrderCommand(
            WorkOrderId: Guid.CreateVersion7(),
            NewStartAt: DateTimeOffset.UtcNow.AddDays(1),
            NewSpot: Spot.B);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}