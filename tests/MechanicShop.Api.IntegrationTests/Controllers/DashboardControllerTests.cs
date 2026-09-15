using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using MechanicShop.Api.IntegrationTests.Common;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Dashboard.Dtos;
using MechanicShop.Domain.Employees;
using MechanicShop.Domain.Workorders;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Tests.Common.Customers;
using MechanicShop.Tests.Common.Employees;
using MechanicShop.Tests.Common.RepairTasks;
using MechanicShop.Tests.Common.Security;
using MechanicShop.Tests.Common.WorkOrders;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class DashboardControllerTests : BaseIntegrationTest
{
    private readonly WebAppFactory _factory;

    public DashboardControllerTests(WebAppFactory webAppFactory)
        : base(webAppFactory)
    {
        _factory = webAppFactory;
    }

    [Fact]
    public async Task GetTodayStats_WhenAuthenticatedWithoutCustomDate_ShouldReturnOkWithStats()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Labor01);
        Client.SetAuthorizationHeader(token);

        using var scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        await SeedWorkOrderForTodayAsync(context);

        // Act
        var response = await Client.GetAsync("/api/v1.0/dashboard/stats");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stats = await response.Content.ReadFromJsonAsync<TodayWorkOrderStatsDto>();

        Assert.NotNull(stats);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), stats!.Date);
        Assert.True(stats.Total > 0, "Total orders should be greater than 0");
        Assert.True(stats.UniqueCustomers > 0, "Unique customers should be greater than 0");
    }

    [Fact]
    public async Task GetTodayStats_WithSpecificDate_ShouldReturnStatsForThatDate()
    {
        // Arrange
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2));
        var dateParam = targetDate.ToString("yyyy-MM-dd");

        // Act
        var response = await Client.GetAsync($"/api/v1.0/dashboard/stats?date={dateParam}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stats = await response.Content.ReadFromJsonAsync<TodayWorkOrderStatsDto>();

        Assert.NotNull(stats);
        Assert.Equal(targetDate, stats!.Date);
    }

    [Fact]
    public async Task GetTodayStats_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        Client.ClearAuthorizationHeader();

        // Act
        var response = await Client.GetAsync("/api/v1.0/dashboard/stats");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<WorkOrder> SeedWorkOrderForTodayAsync(IAppDbContext context)
    {
        using var scope = _factory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customer = CustomerFactory.CreateCustomer().Value;
        var testAppUser = CreateTestAppUser();
        var labor = testAppUser.Employee!;
        var repairTask = RepairTaskFactory.CreateRepairTask().Value;
        var vehicleId = customer.Vehicles.First().Id;

        var identityResult = await userManager.CreateAsync(testAppUser, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.RepairTasks.AddAsync(repairTask);
        await context.SaveChangesAsync(default);

        var workOrder = WorkOrderTestDataBuilder.Create()
                    .WithTimeSlot(DateTimeOffset.UtcNow.Date.AddHours(10), DateTimeOffset.UtcNow.Date.AddHours(11))
                    .WithRepairTasks(WorkOrderTaskFactory.CreateTask(originalTaskId: repairTask.Id))
                    .WithVehicle(vehicleId)
                    .WithLabor(TestUsers.Labor01.Id)
                    .Build();

        await context.WorkOrders.AddAsync(workOrder);
        await context.SaveChangesAsync(default);

        return workOrder;
    }

    private AppUser CreateTestAppUser()
    {
        var localPart = TestUsers.Labor01.Email?.Split('@').FirstOrDefault() ?? "user";
        var parts = localPart.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var first = parts.Length > 0 ? parts[0] : "First";
        var last = parts.Length > 1 ? parts[1] : "Last";

        return EmployeeFactory.CreateLabor(
            id: TestUsers.Labor01.Id,
            firstName: first,
            lastName: last).Value;
    }
}