using FluentAssertions;

using MechanicShop.Application.Features.RepairTasks.Mappers;
using MechanicShop.Domain.Inventory;
using MechanicShop.Domain.RepairTasks;
using MechanicShop.Domain.Workorders;
using MechanicShop.Tests.Common.Inventory;
using MechanicShop.Tests.Common.RepairTasks;
using MechanicShop.Tests.Common.WorkOrders;

using Xunit;

namespace MechanicShop.Application.UnitTests.Mappers;

public class RepairTaskMapperTest
{
    [Fact]
    public void ToDto_WithValidRepairTaskEntity_ShouldMapCorrectly()
    {
        // Arrange
        var inventoryItem = InventoryItemFactory.CreateItem(name: "Engine Oil", cost: 50m).Value;
        var part = RepairTaskPartFactory.CreatePart(inventoryItemId: inventoryItem.Id, quantity: 2).Value;
        var repairTask = RepairTaskFactory.CreateRepairTask(
            name: "Oil Change",
            laborCost: 100m,
            parts: [part]).Value;

        var map = new Dictionary<Guid, InventoryItem> { { inventoryItem.Id, inventoryItem } };
        const decimal expectedTotalCost = 100m + (50m * 2);

        // Act
        var dto = repairTask.ToDto(map);

        // Assert
        dto.Should().NotBeNull();
        dto.OriginalRepairTaskId.Should().Be(repairTask.Id);
        dto.Name.Should().Be(repairTask.Name);
        dto.LaborCost.Should().Be(repairTask.LaborCost);
        dto.TotalCost.Should().Be(expectedTotalCost);
        dto.EstimatedDurationInMins.Should().Be(repairTask.EstimatedDurationInMins);

        dto.Parts.Should().ContainSingle();
        var partDto = dto.Parts[0];
        partDto.InventoryItemId.Should().Be(inventoryItem.Id);
        partDto.Name.Should().Be(inventoryItem.Name);
        partDto.Cost.Should().Be(inventoryItem.Cost);
        partDto.Quantity.Should().Be(part.Quantity);
    }

    [Fact]
    public void ToDtos_WithRepairTaskEnumerable_ShouldMapListCorrectly()
    {
        // Arrange
        var inventoryItem = InventoryItemFactory.CreateItem().Value;
        var part = RepairTaskPartFactory.CreatePart(inventoryItemId: inventoryItem.Id).Value;
        var repairTask = RepairTaskFactory.CreateRepairTask(parts: [part]).Value;
        var map = new Dictionary<Guid, InventoryItem> { { inventoryItem.Id, inventoryItem } };
        var entities = new List<RepairTask> { repairTask };

        // Act
        var dtos = entities.Select(e => e.ToDto(map)).ToList();

        // Assert
        dtos.Should().ContainSingle();
        dtos[0].OriginalRepairTaskId.Should().Be(repairTask.Id);
    }

    [Fact]
    public void ToDto_WithNullRepairTask_ShouldThrowArgumentNullException()
    {
        // Arrange
        RepairTask? entity = null;
        var map = new Dictionary<Guid, InventoryItem>();

        // Act
        Action act = () => entity!.ToDto(map);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToDto_WithValidWorkOrderTaskSnapshot_ShouldMapCorrectly()
    {
        // Arrange
        var orderPart = WorkOrderTaskPartFactory.CreatePart(name: "Spark Plug", cost: 30m, quantity: 4);
        var orderTask = WorkOrderTaskFactory.CreateTask(name: "Tune Up", laborCost: 80m, parts: [orderPart]);

        // Act
        var dto = orderTask.ToDto();

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(orderTask.Id);
        dto.Name.Should().Be(orderTask.Name);
        dto.LaborCost.Should().Be(orderTask.LaborCost);
        dto.TotalCost.Should().Be(orderTask.TotalCost);
        dto.EstimatedDurationInMins.Should().Be(orderTask.EstimatedDurationInMins);

        dto.Parts.Should().ContainSingle();
        var partDto = dto.Parts[0];
        partDto.InventoryItemId.Should().Be(orderPart.InventoryItemId);
        partDto.Name.Should().Be(orderPart.Name);
        partDto.Cost.Should().Be(orderPart.Cost);
        partDto.Quantity.Should().Be(orderPart.Quantity);
    }

    [Fact]
    public void ToDtos_WithWorkOrderTaskEnumerable_ShouldMapListCorrectly()
    {
        // Arrange
        var orderTask = WorkOrderTaskFactory.CreateTask();
        var entities = new List<WorkOrderTask> { orderTask };

        // Act
        var dtos = entities.ToDtos();

        // Assert
        dtos.Should().ContainSingle();
        dtos[0].Id.Should().Be(orderTask.Id);
    }

    [Fact]
    public void ToDto_WithNullWorkOrderTask_ShouldThrowArgumentNullException()
    {
        // Arrange
        WorkOrderTask? entity = null;

        // Act
        Action act = () => entity!.ToDto();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}