using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.RepairTasks.Commands.CreateRepairTask;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.RepairTasks;
using MechanicShop.Domain.RepairTasks.Enums;
using MechanicShop.Tests.Common.Inventory;
using MechanicShop.Tests.Common.RepairTasks;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.RepairTasks.Commands.CreateRepairTask;

[Collection(WebAppFactoryCollection.CollectionName)]
public class CreateRepairTaskCommandTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateRepairTaskAndPartsSuccessfully()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var inventoryItem = InventoryItemFactory.CreateItem(name: "Brake Pad Kit", cost: 45.00m).Value;
        await context.InventoryItems.AddAsync(inventoryItem);
        await context.SaveChangesAsync(default);

        var partCommand = new CreateRepairTaskPartCommand(inventoryItem.Id, 2);
        var command = new CreateRepairTaskCommand(
            Name: "Front Brake Service",
            LaborCost: 80.00m,
            EstimatedDurationInMins: RepairDurationInMinutes.Min60,
            Parts: [partCommand]);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be("Front Brake Service");

        using var assertScope = _factory.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var dbRepairTask = await assertContext.RepairTasks
            .Include(t => t.Parts)
            .FirstOrDefaultAsync(t => t.Id == result.Value.OriginalRepairTaskId);

        dbRepairTask.Should().NotBeNull();
        dbRepairTask!.Name.Should().Be("Front Brake Service");
        dbRepairTask.Parts.Should().ContainSingle();
        dbRepairTask.Parts.First().InventoryItemId.Should().Be(inventoryItem.Id);
    }

    [Fact]
    public async Task Handle_WhenRepairTaskNameExistsCaseInsensitive_ShouldReturnDuplicateError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var inventoryItem = InventoryItemFactory.CreateItem().Value;
        await context.InventoryItems.AddAsync(inventoryItem);

        var existingTask = RepairTaskFactory.CreateRepairTask(name: "A/C Recharge").Value;
        await context.RepairTasks.AddAsync(existingTask);
        await context.SaveChangesAsync(default);

        var partCommand = new CreateRepairTaskPartCommand(inventoryItem.Id, 1);
        var command = new CreateRepairTaskCommand(
            Name: "a/c recharge",
            LaborCost: 60.00m,
            EstimatedDurationInMins: RepairDurationInMinutes.Min30,
            Parts: [partCommand]);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(RepairTaskErrors.DuplicateName.Code);
    }
}