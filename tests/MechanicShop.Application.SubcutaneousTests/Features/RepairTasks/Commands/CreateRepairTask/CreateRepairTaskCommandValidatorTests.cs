using FluentValidation.TestHelper;

using MechanicShop.Domain.RepairTasks.Enums;

using Xunit;

using ChildQuery = MechanicShop.Application.Features.RepairTasks.Commands.CreateRepairTask.CreateRepairTaskPartCommand;
using ParentQuery = MechanicShop.Application.Features.RepairTasks.Commands.CreateRepairTask.CreateRepairTaskCommand;
using ParentValidator = MechanicShop.Application.Features.RepairTasks.Commands.CreateRepairTask.CreateRepairTaskCommandValidator;

namespace MechanicShop.Application.SubcutaneousTests.Features.RepairTasks.Commands.CreateRepairTask;

public class CreateRepairTaskCommandValidatorTests
{
    private readonly ParentValidator _validator;

    public CreateRepairTaskCommandValidatorTests()
    {
        _validator = new ParentValidator();
    }

    [Fact]
    public void Validator_WhenNameIsEmpty_ShouldHaveValidationError()
    {
        var command = new ParentQuery(
            Name: string.Empty,
            LaborCost: 50,
            EstimatedDurationInMins: RepairDurationInMinutes.Min60,
            Parts: [new ChildQuery(Guid.CreateVersion7(), 4)]);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Name is required.");
    }

    [Fact]
    public void Validator_WhenLaborCostIsZeroOrLess_ShouldHaveValidationError()
    {
        var command = new ParentQuery(
            Name: "Engine Oil Change",
            LaborCost: 0,
            EstimatedDurationInMins: RepairDurationInMinutes.Min60,
            Parts: [new ChildQuery(Guid.CreateVersion7(), 1)]);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.LaborCost)
              .WithErrorMessage("Labor cost must be greater than 0.");
    }

    [Fact]
    public void Validator_WhenPartsListIsEmpty_ShouldHaveValidationError()
    {
        var command = new ParentQuery(
            Name: "Brake Replacement",
            LaborCost: 100,
            EstimatedDurationInMins: RepairDurationInMinutes.Min60,
            Parts: []);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Parts)
              .WithErrorMessage("At least one part is required.");
    }

    [Fact]
    public void Validator_WhenChildPartDataIsInvalid_ShouldTriggerChildValidatorErrors()
    {
        var command = new ParentQuery(
            Name: "Brake Replacement",
            LaborCost: 100,
            EstimatedDurationInMins: RepairDurationInMinutes.Min60,
            Parts: [new ChildQuery(InventoryItemId: Guid.Empty, Quantity: 0)]);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Parts[0].InventoryItemId");
        result.ShouldHaveValidationErrorFor("Parts[0].Quantity");
    }

    [Fact]
    public void Validator_WhenAllDataIsValid_ShouldNotHaveValidationErrors()
    {
        var command = new ParentQuery(
            Name: "Transmission Fluid Flush",
            LaborCost: 120,
            EstimatedDurationInMins: RepairDurationInMinutes.Min120,
            Parts: [new ChildQuery(Guid.CreateVersion7(), 1)]);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}