using FluentAssertions;

using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.RepairTasks.Commands.UpdateRepairTask;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.RepairTasks.Enums;
using MechanicShop.Tests.Common.Inventory;
using MechanicShop.Tests.Common.RepairTasks;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.RepairTasks.Commands.UpdateRepairTask;

[Collection(WebAppFactoryCollection.CollectionName)]
public class UpdateRepairTaskCommandTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WhenRepairTaskDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var command = new UpdateRepairTaskCommand(
            RepairTaskId: Guid.CreateVersion7(),
            Name: "Non Existent Task",
            LaborCost: 200.00m,
            EstimatedDurationInMins: RepairDurationInMinutes.Min30,
            Parts: [new UpdateRepairTaskPartCommand(Guid.CreateVersion7(), 4)]);

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsError.Should().BeTrue();
        result.TopError.Code.Should().Be(ApplicationErrors.RepairTasks.NotFound.Code);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateRepairTaskAndPartsSuccessfully()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var inventoryItem = InventoryItemFactory.CreateItem(name: "Oil Filter", cost: 20.00m).Value;
        await context.InventoryItems.AddAsync(inventoryItem);

        var taskId = Guid.CreateVersion7();
        var existingTask = RepairTaskFactory.CreateRepairTask(taskId, "Old Engine Check", 100.00m, RepairDurationInMinutes.Min30, []).Value;
        await context.RepairTasks.AddAsync(existingTask);
        await context.SaveChangesAsync(default);

        var partCommand = new UpdateRepairTaskPartCommand(inventoryItem.Id, 1);
        var command = new UpdateRepairTaskCommand(
            RepairTaskId: taskId,
            Name: "Advanced Engine TuneUp",
            LaborCost: 350.00m,
            EstimatedDurationInMins: RepairDurationInMinutes.Min60,
            Parts: [partCommand]);

        try
        {
            // Act
            var result = await mediator.Send(command);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(Result.Updated);

            using var assertScope = _factory.CreateScope();
            var assertContext = assertScope.ServiceProvider.GetRequiredService<IAppDbContext>();

            var dbTask = await assertContext.RepairTasks
                .Include(rt => rt.Parts)
                .FirstOrDefaultAsync(rt => rt.Id == taskId);

            dbTask.Should().NotBeNull();
            dbTask!.Name.Should().Be("Advanced Engine TuneUp");
            dbTask.LaborCost.Should().Be(350.00m);
            dbTask.EstimatedDurationInMins.Should().Be(RepairDurationInMinutes.Min60);

            dbTask.Parts.Should().ContainSingle();
            dbTask.Parts.First().InventoryItemId.Should().Be(inventoryItem.Id);
        }
        finally
        {
            await context.RepairTasks.Where(rt => rt.Id == taskId).ExecuteDeleteAsync();
        }
    }
}