using FluentValidation.TestHelper;

using MechanicShop.Application.Features.WorkOrders.Commands.CreateWorkOrder;
using MechanicShop.Domain.Workorders.Enums;

using Xunit;

namespace MechanicShop.Application.UnitTests.Validators;

public class CreateWorkOrderCommandValidatorTests
{
    private readonly CreateWorkOrderCommandValidator _validator;

    public CreateWorkOrderCommandValidatorTests()
    {
        _validator = new CreateWorkOrderCommandValidator();
    }

    [Fact]
    public void Validator_WhenVehicleIdIsEmpty_ShouldHaveValidationError()
    {
        var command = new CreateWorkOrderCommand(
            Spot: Spot.A,
            VehicleId: Guid.Empty,
            StartAt: DateTimeOffset.UtcNow.AddHours(1),
            RepairTaskIds: [Guid.CreateVersion7()],
            LaborId: null);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.VehicleId)
              .WithErrorMessage("VehicleId is required.");
    }

    [Fact]
    public void Validator_WhenVehicleIdIsValid_ShouldNotHaveValidationError()
    {
        var command = new CreateWorkOrderCommand(
            Spot: Spot.A,
            VehicleId: Guid.CreateVersion7(),
            StartAt: DateTimeOffset.UtcNow.AddHours(1),
            RepairTaskIds: [Guid.CreateVersion7()],
            LaborId: null);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.VehicleId);
    }

    [Fact]
    public void Validator_WhenStartAtIsInThePast_ShouldHaveValidationError()
    {
        var command = new CreateWorkOrderCommand(
            Spot: Spot.A,
            VehicleId: Guid.CreateVersion7(),
            StartAt: DateTimeOffset.UtcNow.AddMinutes(-5),
            RepairTaskIds: [Guid.CreateVersion7()],
            LaborId: null);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.StartAt)
              .WithErrorMessage("StartAt must be in the future.");
    }

    [Fact]
    public void Validator_WhenStartAtIsInTheFuture_ShouldNotHaveValidationError()
    {
        var command = new CreateWorkOrderCommand(
            Spot: Spot.A,
            VehicleId: Guid.CreateVersion7(),
            StartAt: DateTimeOffset.UtcNow.AddHours(2),
            RepairTaskIds: [Guid.CreateVersion7()],
            LaborId: null);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.StartAt);
    }

    [Fact]
    public void Validator_WhenRepairTaskIdsIsEmpty_ShouldHaveValidationError()
    {
        var command = new CreateWorkOrderCommand(
            Spot: Spot.A,
            VehicleId: Guid.CreateVersion7(),
            StartAt: DateTimeOffset.UtcNow.AddHours(1),
            RepairTaskIds: [],
            LaborId: null);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.RepairTaskIds)
              .WithErrorMessage("At least one repair task must be selected");
    }

    [Fact]
    public void Validator_WhenRepairTaskIdsHasItems_ShouldNotHaveValidationError()
    {
        var command = new CreateWorkOrderCommand(
            Spot: Spot.A,
            VehicleId: Guid.CreateVersion7(),
            StartAt: DateTimeOffset.UtcNow.AddHours(1),
            RepairTaskIds: [Guid.CreateVersion7()],
            LaborId: null);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.RepairTaskIds);
    }

    [Fact]
    public void Validator_WhenLaborIdIsEmptyGuid_ShouldHaveValidationError()
    {
        var command = new CreateWorkOrderCommand(
            Spot: Spot.A,
            VehicleId: Guid.CreateVersion7(),
            StartAt: DateTimeOffset.UtcNow.AddHours(1),
            RepairTaskIds: [Guid.CreateVersion7()],
            LaborId: Guid.Empty);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.LaborId)
              .WithErrorMessage("If provided, LaborId must not be empty.");
    }

    [Fact]
    public void Validator_WhenLaborIdIsNull_ShouldNotHaveValidationError()
    {
        var command = new CreateWorkOrderCommand(
            Spot: Spot.A,
            VehicleId: Guid.CreateVersion7(),
            StartAt: DateTimeOffset.UtcNow.AddHours(1),
            RepairTaskIds: [Guid.CreateVersion7()],
            LaborId: null);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.LaborId);
    }

    [Fact]
    public void Validator_WhenLaborIdIsValid_ShouldNotHaveValidationError()
    {
        var command = new CreateWorkOrderCommand(
            Spot: Spot.A,
            VehicleId: Guid.CreateVersion7(),
            StartAt: DateTimeOffset.UtcNow.AddHours(1),
            RepairTaskIds: [Guid.CreateVersion7()],
            LaborId: Guid.CreateVersion7());

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.LaborId);
    }

    [Fact]
    public void Validator_WhenSpotIsInvalidEnum_ShouldHaveValidationError()
    {
        var command = new CreateWorkOrderCommand(
            Spot: (Spot)99,
            VehicleId: Guid.CreateVersion7(),
            StartAt: DateTimeOffset.UtcNow.AddHours(1),
            RepairTaskIds: [Guid.CreateVersion7()],
            LaborId: null);

        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Spot)
              .WithErrorCode("Spot_Invalid")
              .WithErrorMessage("Spot must be a valid Spot value. [A, B, C, D]");
    }

    [Fact]
    public void Validator_WhenSpotIsValidEnum_ShouldNotHaveValidationError()
    {
        var command = new CreateWorkOrderCommand(
            Spot: Spot.A,
            VehicleId: Guid.CreateVersion7(),
            StartAt: DateTimeOffset.UtcNow.AddHours(1),
            RepairTaskIds: [Guid.CreateVersion7()],
            LaborId: null);

        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.Spot);
    }
}