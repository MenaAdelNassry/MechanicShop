using System.Net;
using System.Net.Http.Json;

using MechanicShop.Api.IntegrationTests.Common;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Api.Requests.RepairTasks;
using MechanicShop.Tests.Common.Inventory;
using MechanicShop.Tests.Common.RepairTasks;
using MechanicShop.Tests.Common.Security;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class RepairTasksControllerTests : BaseIntegrationTest
{
    public RepairTasksControllerTests(WebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task Get_ShouldReturnAllRepairTasksWithParts()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var inventoryItem = InventoryItemFactory.CreateItem().Value;
        var part1 = RepairTaskPartFactory.CreatePart(inventoryItemId: inventoryItem.Id).Value;
        var part2 = RepairTaskPartFactory.CreatePart(inventoryItemId: inventoryItem.Id).Value;

        var task1 = RepairTaskFactory.CreateRepairTask(parts: [part1]).Value;
        var task2 = RepairTaskFactory.CreateRepairTask(parts: [part2]).Value;

        using (var scope = Factory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            await context.InventoryItems.AddAsync(inventoryItem);
            await context.RepairTasks.AddRangeAsync(task1, task2);
            await context.SaveChangesAsync(default);
        }

        // Act
        var response = await Client.GetAsync("/api/v1.0/repair-tasks");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dtos = await response.Content.ReadFromJsonAsync<List<RepairTaskDto>>();
        Assert.NotNull(dtos);
        Assert.True(dtos!.Count >= 2);
        Assert.Contains(dtos, d => d.OriginalRepairTaskId == task1.Id);
        Assert.Contains(dtos, d => d.OriginalRepairTaskId == task2.Id);
        dtos.ForEach(d => Assert.NotNull(d.Parts));
    }

    [Fact]
    public async Task Get_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        Client.ClearAuthorizationHeader();

        // Act
        var response = await Client.GetAsync("/api/v1.0/repair-tasks");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithValidId_ShouldReturnRepairTaskWithParts()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var inventoryItem = InventoryItemFactory.CreateItem().Value;
        var part = RepairTaskPartFactory.CreatePart(inventoryItemId: inventoryItem.Id).Value;
        var task = RepairTaskFactory.CreateRepairTask(parts: [part]).Value;

        using (var scope = Factory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            await context.InventoryItems.AddAsync(inventoryItem);
            await context.RepairTasks.AddAsync(task);
            await context.SaveChangesAsync(default);
        }

        // Act
        var response = await Client.GetAsync($"/api/v1.0/repair-tasks/{task.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<RepairTaskDto>();
        Assert.NotNull(dto);
        Assert.Equal(task.Id, dto!.OriginalRepairTaskId);
        Assert.NotNull(dto.Parts);
    }

    [Fact]
    public async Task Create_AsManager_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var inventoryItem = InventoryItemFactory.CreateItem().Value;

        using (var scope = Factory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            await context.InventoryItems.AddAsync(inventoryItem);
            await context.SaveChangesAsync(default);
        }

        var request = new CreateRepairTaskRequest
        {
            Name = $"Test Repair {Guid.CreateVersion7()}",
            LaborCost = 150M,
            EstimatedDurationInMins = Contracts.Common.RepairDurationInMinutes.Min30,
            Parts = new List<CreateRepairTaskPartRequest>
            {
                new() { InventoryItemId = inventoryItem.Id, Quantity = 1 }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1.0/repair-tasks", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<RepairTaskDto>();
        Assert.NotNull(dto);
        Assert.Equal(request.Name, dto!.Name);
        Assert.NotNull(dto.Parts);
        Assert.NotEmpty(dto.Parts);
    }

    [Fact]
    public async Task Create_AsLabor_ShouldReturnForbidden()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Labor01);
        Client.SetAuthorizationHeader(token);

        var request = new CreateRepairTaskRequest
        {
            Name = "Unauthorized Create",
            LaborCost = 10M,
            EstimatedDurationInMins = Contracts.Common.RepairDurationInMinutes.Min15,
            Parts = new List<CreateRepairTaskPartRequest>()
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1.0/repair-tasks", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_AsManager_WithValidRequest_ShouldReturnNoContentAndPersistChanges()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var inventoryItem = InventoryItemFactory.CreateItem().Value;
        var part = RepairTaskPartFactory.CreatePart(inventoryItemId: inventoryItem.Id).Value;
        var existing = RepairTaskFactory.CreateRepairTask(parts: [part]).Value;

        using (var scope = Factory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            await context.InventoryItems.AddAsync(inventoryItem);
            await context.RepairTasks.AddAsync(existing);
            await context.SaveChangesAsync(default);
        }

        var updateRequest = new UpdateRepairTaskRequest
        {
            Name = $"Updated Name {Guid.CreateVersion7()}",
            LaborCost = existing.LaborCost + 10,
            EstimatedDurationInMins = Contracts.Common.RepairDurationInMinutes.Min60,
            Parts = existing.Parts.Select(p => new UpdateRepairTaskPartRequest
            {
                InventoryItemId = p.InventoryItemId,
                Quantity = p.Quantity
            }).ToList()
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/v1.0/repair-tasks/{existing.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using (var scope = Factory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var updated = await context.RepairTasks.Include(r => r.Parts).FirstOrDefaultAsync(r => r.Id == existing.Id);
            Assert.NotNull(updated);
            Assert.Equal(updateRequest.Name, updated!.Name);
            Assert.Equal(updateRequest.LaborCost, updated.LaborCost);
        }
    }

    [Fact]
    public async Task Delete_AsManager_WithValidId_ShouldReturnNoContentAndRemoveEntity()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var inventoryItem = InventoryItemFactory.CreateItem().Value;
        var part = RepairTaskPartFactory.CreatePart(inventoryItemId: inventoryItem.Id).Value;
        var existing = RepairTaskFactory.CreateRepairTask(parts: [part]).Value;

        using (var scope = Factory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            await context.InventoryItems.AddAsync(inventoryItem);
            await context.RepairTasks.AddAsync(existing);
            await context.SaveChangesAsync(default);
        }

        // Act
        var response = await Client.DeleteAsync($"/api/v1.0/repair-tasks/{existing.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using (var scope = Factory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var found = await context.RepairTasks.FindAsync(existing.Id);
            Assert.Null(found);
        }
    }

    [Fact]
    public async Task Delete_AsLabor_ShouldReturnForbidden()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Labor01);
        Client.SetAuthorizationHeader(token);

        var inventoryItem = InventoryItemFactory.CreateItem().Value;
        var part = RepairTaskPartFactory.CreatePart(inventoryItemId: inventoryItem.Id).Value;
        var existing = RepairTaskFactory.CreateRepairTask(parts: [part]).Value;

        using (var scope = Factory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            await context.InventoryItems.AddAsync(inventoryItem);
            await context.RepairTasks.AddAsync(existing);
            await context.SaveChangesAsync(default);
        }

        // Act
        var response = await Client.DeleteAsync($"/api/v1.0/repair-tasks/{existing.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}