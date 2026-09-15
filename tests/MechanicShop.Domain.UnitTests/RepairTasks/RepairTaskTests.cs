using FluentAssertions;

using MechanicShop.Domain.RepairTasks;
using MechanicShop.Domain.RepairTasks.Enums;
using MechanicShop.Tests.Common.RepairTasks;

using Xunit;

namespace MechanicShop.Domain.UnitTests.RepairTasks;

public class RepairTaskTests
{
    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var id = Guid.CreateVersion7();
        const string name = "SomeTask";
        const decimal laborCost = 100m;
        const RepairDurationInMinutes estimatedDurationInMin = RepairDurationInMinutes.Min30;
        List<RepairTaskPart> parts = [RepairTaskPartFactory.CreatePart().Value];

        // Act
        var result = RepairTask.Create(
            id: id,
            name: name,
            laborCost: laborCost,
            estimatedDurationInMins: estimatedDurationInMin,
            parts: parts);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var task = result.Value;
        task.Id.Should().Be(id);
        task.Name.Should().Be(name);
        task.LaborCost.Should().Be(laborCost);
        task.EstimatedDurationInMins.Should().Be(estimatedDurationInMin);
        task.Parts.Should().ContainSingle();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldFail()
    {
        // Act
        var result = RepairTask.Create(
            id: Guid.CreateVersion7(),
            name: " ",
            laborCost: 100m,
            estimatedDurationInMins: RepairDurationInMinutes.Min30,
            parts: [RepairTaskPartFactory.CreatePart().Value]);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(RepairTaskErrors.NameRequired.Code);
    }

    [Fact]
    public void Create_WithInvalidLaborCost_ShouldFail()
    {
        // Act
        var result = RepairTask.Create(
            id: Guid.CreateVersion7(),
            name: "Brake Inspection",
            laborCost: 0m,
            estimatedDurationInMins: RepairDurationInMinutes.Min30,
            parts: [RepairTaskPartFactory.CreatePart().Value]);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(RepairTaskErrors.LaborCostInvalid.Code);
    }

    [Fact]
    public void Create_WithInvalidDuration_ShouldFail()
    {
        // Arrange
        const RepairDurationInMinutes invalidDurationValue = (RepairDurationInMinutes)999;

        // Act
        var result = RepairTask.Create(
            id: Guid.CreateVersion7(),
            name: "Brake Inspection",
            laborCost: 100m,
            estimatedDurationInMins: invalidDurationValue,
            parts: [RepairTaskPartFactory.CreatePart().Value]);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(RepairTaskErrors.DurationInvalid.Code);
    }

    [Fact]
    public void UpsertParts_AddsNewPart_WhenNotExisting()
    {
        // Arrange
        var task = RepairTaskFactory.CreateRepairTask().Value;
        var incoming = RepairTaskPartFactory.CreatePart().Value;

        // Act
        var result = task.UpsertParts([incoming]);

        // Assert
        result.IsSuccess.Should().BeTrue();
        task.Parts.Should().Contain(p => p.InventoryItemId == incoming.InventoryItemId);
    }

    [Fact]
    public void UpsertParts_UpdatesExistingPartQuantity_WhenExisting()
    {
        // Arrange
        var inventoryItemId = Guid.CreateVersion7();
        var original = RepairTaskPartFactory.CreatePart(inventoryItemId: inventoryItemId, quantity: 2).Value;
        var task = RepairTaskFactory.CreateRepairTask(parts: [original]).Value;
        var incoming = RepairTaskPartFactory.CreatePart(inventoryItemId: inventoryItemId, quantity: 5).Value;

        // Act
        var result = task.UpsertParts([incoming]);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updated = task.Parts.First(p => p.InventoryItemId == inventoryItemId);
        updated.Quantity.Should().Be(5);
    }

    [Fact]
    public void UpsertParts_RemovesMissingParts()
    {
        // Arrange
        var keep = RepairTaskPartFactory.CreatePart().Value;
        var remove = RepairTaskPartFactory.CreatePart().Value;
        var task = RepairTaskFactory.CreateRepairTask(parts: [keep, remove]).Value;

        // Act
        var result = task.UpsertParts([keep]);

        // Assert
        result.IsSuccess.Should().BeTrue();
        task.Parts.Should().ContainSingle().Which.InventoryItemId.Should().Be(keep.InventoryItemId);
    }

    [Fact]
    public void Update_ShouldReturnSuccess_WithValidValues()
    {
        // Arrange
        var task = RepairTaskFactory.CreateRepairTask().Value;

        // Act
        var result = task.Update("Valid", 123m, RepairDurationInMinutes.Min30);

        // Assert
        result.IsSuccess.Should().BeTrue();
        task.Name.Should().Be("Valid");
        task.LaborCost.Should().Be(123m);
        task.EstimatedDurationInMins.Should().Be(RepairDurationInMinutes.Min30);
    }

    [Theory]
    [InlineData("", 1, RepairDurationInMinutes.Min30)]
    [InlineData("  ", 1, RepairDurationInMinutes.Min30)]
    [InlineData("Name", 0, RepairDurationInMinutes.Min30)]
    [InlineData("Name", 10001, RepairDurationInMinutes.Min30)]
    public void Update_ShouldReturnError_ForInvalidNameOrCost(string name, decimal cost, RepairDurationInMinutes dur)
    {
        // Arrange
        var task = RepairTaskFactory.CreateRepairTask().Value;

        // Act
        var result = task.Update(name, cost, dur);

        // Assert
        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void Update_ShouldReturnError_ForInvalidDuration()
    {
        // Arrange
        var task = RepairTaskFactory.CreateRepairTask().Value;
        const RepairDurationInMinutes invalid = (RepairDurationInMinutes)999;

        // Act
        var result = task.Update("Name", 1m, invalid);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.TopError.Code.Should().Be(RepairTaskErrors.DurationInvalid.Code);
    }
}