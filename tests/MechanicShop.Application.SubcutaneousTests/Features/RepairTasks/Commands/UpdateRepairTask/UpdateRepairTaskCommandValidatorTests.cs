using FluentValidation.TestHelper;

using MechanicShop.Application.Features.RepairTasks.Commands.UpdateRepairTask;
using MechanicShop.Domain.RepairTasks.Enums;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.RepairTasks.Commands.UpdateRepairTask;

public class UpdateRepairTaskCommandValidatorTests
{
    private readonly UpdateRepairTaskCommandValidator _validator;

    public UpdateRepairTaskCommandValidatorTests()
    {
        _validator = new UpdateRepairTaskCommandValidator();
    }

    [Fact]
    public void Validator_WhenRepairTaskIdIsEmpty_ShouldHaveValidationError()
    {
        var command = new UpdateRepairTaskCommand(
            RepairTaskId: Guid.Empty,
            Name: "Engine Check",
            LaborCost: 150.00m,
            EstimatedDurationInMins: RepairDurationInMinutes.Min60,
            Parts: [new UpdateRepairTaskPartCommand(Guid.CreateVersion7(), 1)]);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RepairTaskId)
              .WithErrorMessage("Repair task ID is required.");
    }

    [Fact]
    public void Validator_WhenLaborCostIsOutOfRange_ShouldHaveValidationError()
    {
        var command = new UpdateRepairTaskCommand(
            RepairTaskId: Guid.CreateVersion7(),
            Name: "Engine Check",
            LaborCost: 20_000.00m, // Max allowed is 10,000
            EstimatedDurationInMins: RepairDurationInMinutes.Min60,
            Parts: [new UpdateRepairTaskPartCommand(Guid.CreateVersion7(), 1)]);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.LaborCost)
              .WithErrorMessage("Labor cost must be between 1 and 10,000.");
    }

    [Fact]
    public void Validator_WhenPartsListIsNull_ShouldHaveValidationError()
    {
        var command = new UpdateRepairTaskCommand(
            RepairTaskId: Guid.CreateVersion7(),
            Name: "Engine Check",
            LaborCost: 150.00m,
            EstimatedDurationInMins: RepairDurationInMinutes.Min60,
            Parts: null!);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Parts)
              .WithErrorMessage("Parts list cannot be null.");
    }

    [Fact]
    public void Validator_WhenChildPartPropertiesAreInvalid_ShouldTriggerChildValidator()
    {
        var command = new UpdateRepairTaskCommand(
            RepairTaskId: Guid.CreateVersion7(),
            Name: "Engine Check",
            LaborCost: 150.00m,
            EstimatedDurationInMins: RepairDurationInMinutes.Min60,
            Parts: [new UpdateRepairTaskPartCommand(Guid.Empty, 0)]);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Parts[0].InventoryItemId")
              .WithErrorMessage("InventoryItem ID is required.");

        result.ShouldHaveValidationErrorFor("Parts[0].Quantity")
              .WithErrorMessage("Quantity must be at least 1.");
    }

    [Fact]
    public void Validator_WhenAllValuesAreValid_ShouldNotHaveValidationErrors()
    {
        var command = new UpdateRepairTaskCommand(
            RepairTaskId: Guid.CreateVersion7(),
            Name: "A/C Condenser Swapping",
            LaborCost: 250.00m,
            EstimatedDurationInMins: RepairDurationInMinutes.Min90,
            Parts: [new UpdateRepairTaskPartCommand(Guid.CreateVersion7(), 1)]);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}