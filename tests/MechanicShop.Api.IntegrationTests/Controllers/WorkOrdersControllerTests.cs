using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using MechanicShop.Api.IntegrationTests.Common;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Common.Models;
using MechanicShop.Application.Features.Scheduling.Dtos;
using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Api.Requests.WorkOrders;
using MechanicShop.Domain.Workorders;
using MechanicShop.Domain.Workorders.Enums;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Tests.Common.Customers;
using MechanicShop.Tests.Common.Employees;
using MechanicShop.Tests.Common.RepairTasks;
using MechanicShop.Tests.Common.Security;
using MechanicShop.Tests.Common.WorkOrders;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class WorkOrdersControllerTests : BaseIntegrationTest
{
    public WorkOrdersControllerTests(WebAppFactory webAppFactory)
        : base(webAppFactory)
    {
    }

    [Fact]
    public async Task GetWorkOrders_WithValidPagination_ShouldReturnPaginatedList()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var response = await Client.GetAsync("/api/v1.0/workorders?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PaginatedList<WorkOrderListItemDto>>();
        Assert.NotNull(result);
        Assert.NotNull(result!.Items);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Theory]
    [InlineData(0, 10, "Page must be greater than 0")]
    [InlineData(-1, 10, "Page must be greater than 0")]
    [InlineData(1, 0, "PageSize must be between 1 and 100")]
    [InlineData(1, 101, "PageSize must be between 1 and 100")]
    [InlineData(1, -1, "PageSize must be between 1 and 100")]
    public async Task GetWorkOrders_WithInvalidPagination_ShouldReturnBadRequest(int page, int pageSize, string expectedError)
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var response = await Client.GetAsync($"/api/v1.0/workorders?page={page}&pageSize={pageSize}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(expectedError, content);
    }

    [Fact]
    public async Task GetWorkOrders_WithFilters_ShouldApplyFiltersCorrectly()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var vehicleId = Guid.CreateVersion7();
        var laborId = Guid.CreateVersion7();
        const string searchTerm = "test";
        const int state = (int)WorkOrderState.InProgress;
        const int spot = (int)Contracts.Common.Spot.A;
        var startDateFrom = DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        var startDateTo = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var queryString = $"page=1&pageSize=10&searchTerm={searchTerm}&state={state}&vehicleId={vehicleId}&laborId={laborId}&spot={spot}&startDateFrom={startDateFrom}&startDateTo={startDateTo}";

        var response = await Client.GetAsync($"/api/v1.0/workorders?{queryString}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PaginatedList<WorkOrderListItemDto>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetWorkOrders_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        Client.ClearAuthorizationHeader();
        var response = await Client.GetAsync("/api/v1.0/workorders?page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkOrderById_WithValidId_ShouldReturnWorkOrder()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        using var scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var seeded = await SeedWorkOrderWithDependenciesAsync(context, userManager);

        var response = await Client.GetAsync($"/api/v1.0/workorders/{seeded.WorkOrder.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<WorkOrderDto>();
        Assert.NotNull(result);
        Assert.Equal(seeded.WorkOrder.Id, result!.WorkOrderId);
    }

    [Fact]
    public async Task GetWorkOrderById_WithInvalidId_ShouldReturnNotFound()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var nonExistentId = Guid.CreateVersion7();
        var response = await Client.GetAsync($"/api/v1.0/workorders/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetWorkOrderById_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        Client.ClearAuthorizationHeader();
        var workOrderId = Guid.CreateVersion7();
        var response = await Client.GetAsync($"/api/v1.0/workorders/{workOrderId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkOrder_WithValidRequest_ShouldCreateWorkOrder()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        using var scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var testAppUser = CreateTestAppUser();
        var labor = testAppUser.Employee!;
        var customer = CustomerFactory.CreateCustomer().Value;
        var vehicleId = customer.Vehicles.FirstOrDefault()?.Id ?? Guid.CreateVersion7();

        var repairTask1 = RepairTaskFactory.CreateRepairTask().Value;
        var repairTask2 = RepairTaskFactory.CreateRepairTask(name: "Oil Change", laborCost: 200).Value;

        var identityResult = await userManager.CreateAsync(testAppUser, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.RepairTasks.AddRangeAsync(repairTask1, repairTask2);
        await context.SaveChangesAsync(default);

        var repairTaskIds = new List<Guid> { repairTask1.Id, repairTask2.Id };

        var request = new CreateWorkOrderRequest
        {
            Spot = Contracts.Common.Spot.B,
            VehicleId = vehicleId,
            StartAtUtc = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(12),
            LaborId = labor.Id,
            RepairTaskIds = repairTaskIds
        };

        var response = await Client.PostAsJsonAsync("/api/v1.0/workorders", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<WorkOrderDto>();
        Assert.NotNull(dto);
    }

    [Fact]
    public async Task CreateWorkOrder_WithInvalidRequest_ShouldReturnBadRequest()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var request = new CreateWorkOrderRequest
        {
            VehicleId = Guid.Empty,
            StartAtUtc = default,
            RepairTaskIds = []
        };

        var response = await Client.PostAsJsonAsync("/api/v1.0/workorders", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkOrder_WithoutManagerRole_ShouldReturnForbidden()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Labor02);
        Client.SetAuthorizationHeader(token);

        var request = new CreateWorkOrderRequest
        {
            Spot = Contracts.Common.Spot.A,
            VehicleId = Guid.CreateVersion7(),
            StartAtUtc = DateTime.UtcNow.AddHours(1),
            RepairTaskIds = [Guid.CreateVersion7()],
            LaborId = Guid.CreateVersion7()
        };

        var response = await Client.PostAsJsonAsync("/api/v1.0/workorders", request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RelocateWorkOrder_WithValidRequest_ShouldUpdateWorkOrder()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        using var scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var seeded = await SeedWorkOrderWithDependenciesAsync(context, userManager);

        var request = new RelocateWorkOrderRequest
        {
            NewStartAtUtc = DateTime.UtcNow.AddHours(2),
            NewSpot = Contracts.Common.Spot.B
        };

        var response = await Client.PutAsJsonAsync($"/api/v1.0/workorders/{seeded.WorkOrder.Id}/relocation", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RelocateWorkOrder_WithInvalidId_ShouldReturnNotFound()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var nonExistentId = Guid.CreateVersion7();
        var request = new RelocateWorkOrderRequest
        {
            NewStartAtUtc = DateTime.UtcNow.AddHours(2),
            NewSpot = Contracts.Common.Spot.B
        };

        var response = await Client.PutAsJsonAsync($"/api/v1.0/workorders/{nonExistentId}/relocation", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignLabor_WithValidRequest_ShouldAssignLabor()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        using var scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var seeded = await SeedWorkOrderWithDependenciesAsync(context, userManager);

        var request = new AssignLaborRequest
        {
            LaborId = TestUsers.Labor01.Id
        };

        var response = await Client.PutAsJsonAsync($"/api/v1.0/workorders/{seeded.WorkOrder.Id}/labor", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AssignLabor_WithInvalidLaborId_ShouldReturnBadRequest()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var request = new AssignLaborRequest
        {
            LaborId = Guid.Empty
        };

        var response = await Client.PutAsJsonAsync($"/api/v1.0/workorders/{Guid.CreateVersion7()}/labor", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateWorkOrderState_WithValidRequest_ShouldUpdateState()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        using var scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var seeded = await SeedWorkOrderWithDependenciesAsync(context, userManager);

        var request = new UpdateWorkOrderStateRequest
        {
            State = Contracts.Common.WorkOrderState.InProgress
        };

        var response = await Client.PutAsJsonAsync($"/api/v1.0/workorders/{seeded.WorkOrder.Id}/state", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateWorkOrderState_AsLabor_WithSelfScopedAccess_ShouldSucceed()
    {
        using var scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var seeded = await SeedWorkOrderWithDependenciesAsync(context, userManager);

        var token = await Client.GenerateTokenAsync(seeded.User, "SecurePassword123!");
        Client.SetAuthorizationHeader(token);

        var request = new UpdateWorkOrderStateRequest
        {
            State = Contracts.Common.WorkOrderState.InProgress
        };

        var response = await Client.PutAsJsonAsync($"/api/v1.0/workorders/{seeded.WorkOrder.Id}/state", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateWorkOrderState_AsLabor_WithoutSelfScopedAccess_ShouldReturnForbidden()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Labor02);
        Client.SetAuthorizationHeader(token);

        using var scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var seeded = await SeedWorkOrderWithDependenciesAsync(context, userManager);

        var request = new UpdateWorkOrderStateRequest
        {
            State = Contracts.Common.WorkOrderState.InProgress
        };

        var response = await Client.PutAsJsonAsync($"/api/v1.0/workorders/{seeded.WorkOrder.Id}/state", request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRepairTasks_WithValidRequest_ShouldUpdateTasks()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        using var scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var seeded = await SeedWorkOrderWithDependenciesAsync(context, userManager);

        var newRepairTask = await context.RepairTasks.AsNoTracking().FirstOrDefaultAsync();
        var newTaskId = newRepairTask?.Id ?? Guid.CreateVersion7();

        var request = new ModifyRepairTaskRequest
        {
            RepairTaskIds = [newTaskId]
        };

        var response = await Client.PutAsJsonAsync($"/api/v1.0/workorders/{seeded.WorkOrder.Id}/repair-task", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRepairTasks_WithoutManagerRole_ShouldReturnForbidden()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Labor01);
        Client.SetAuthorizationHeader(token);

        var workOrderId = Guid.CreateVersion7();
        var request = new ModifyRepairTaskRequest
        {
            RepairTaskIds = [Guid.CreateVersion7()]
        };

        var response = await Client.PutAsJsonAsync($"/api/v1.0/workorders/{workOrderId}/repair-task", request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkOrder_WithValidId_ShouldDeleteWorkOrder()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        using var scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var seeded = await SeedWorkOrderWithDependenciesAsync(context, userManager);

        var response = await Client.DeleteAsync($"/api/v1.0/workorders/{seeded.WorkOrder.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkOrder_WithInvalidId_ShouldReturnNotFound()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var nonExistentId = Guid.CreateVersion7();
        var response = await Client.DeleteAsync($"/api/v1.0/workorders/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkOrder_WithoutManagerRole_ShouldReturnForbidden()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Labor01);
        Client.SetAuthorizationHeader(token);

        var workOrderId = Guid.CreateVersion7();
        var response = await Client.DeleteAsync($"/api/v1.0/workorders/{workOrderId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetSchedule_WithSpecificDate_ShouldReturnSchedule()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Labor01);
        Client.SetAuthorizationHeader(token);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1.0/workorders/schedule/{today:yyyy-MM-dd}");
        request.Headers.Add("X-TimeZone", "America/Montreal");

        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ScheduleDto>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetSchedule_WithLaborFilter_ShouldReturnFilteredSchedule()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Labor01);
        Client.SetAuthorizationHeader(token);

        using var scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var seeded = await SeedWorkOrderWithDependenciesAsync(context, userManager);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1.0/workorders/schedule/{today:yyyy-MM-dd}");
        request.Headers.Add("X-TimeZone", "America/Montreal");

        var response = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<ScheduleDto>();
        Assert.NotNull(result);
        Assert.NotNull(result.Spots);
    }

    [Fact]
    public async Task AssignLabor_WithNonExistingWorkOrder_ShouldReturnNotFound()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var request = new AssignLaborRequest { LaborId = TestUsers.Labor01.Id };
        var nonExistentId = Guid.CreateVersion7();

        var response = await Client.PutAsJsonAsync($"/api/v1.0/workorders/{nonExistentId}/labor", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AssignLabor_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        Client.ClearAuthorizationHeader();
        var request = new AssignLaborRequest { LaborId = TestUsers.Labor01.Id };
        var workOrderId = Guid.CreateVersion7();

        var response = await Client.PutAsJsonAsync($"/api/v1.0/workorders/{workOrderId}/labor", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateWorkOrderState_WithInvalidWorkOrderId_ShouldReturnNotFound()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var request = new UpdateWorkOrderStateRequest
        {
            State = Contracts.Common.WorkOrderState.InProgress
        };

        var response = await Client.PutAsJsonAsync($"/api/v1.0/workorders/{Guid.CreateVersion7()}/state", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateWorkOrderState_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        Client.ClearAuthorizationHeader();
        var request = new UpdateWorkOrderStateRequest
        {
            State = Contracts.Common.WorkOrderState.InProgress
        };

        var response = await Client.PutAsJsonAsync($"/api/v1.0/workorders/{Guid.CreateVersion7()}/state", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRepairTasks_WithInvalidWorkOrderId_ShouldReturnNotFound()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var request = new ModifyRepairTaskRequest
        {
            RepairTaskIds = [Guid.CreateVersion7()]
        };

        var response = await Client.PutAsJsonAsync($"/api/v1.0/workorders/{Guid.CreateVersion7()}/repair-task", request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateRepairTasks_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        Client.ClearAuthorizationHeader();
        var request = new ModifyRepairTaskRequest
        {
            RepairTaskIds = [Guid.CreateVersion7()]
        };

        var response = await Client.PutAsJsonAsync($"/api/v1.0/workorders/{Guid.CreateVersion7()}/repair-task", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteWorkOrder_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        Client.ClearAuthorizationHeader();
        var response = await Client.DeleteAsync($"/api/v1.0/workorders/{Guid.CreateVersion7()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSchedule_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        Client.ClearAuthorizationHeader();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1.0/workorders/schedule/{today:yyyy-MM-dd}");
        request.Headers.Add("X-TimeZone", "America/Montreal");

        var response = await Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<(WorkOrder WorkOrder, AppUser User)> SeedWorkOrderWithDependenciesAsync(IAppDbContext context, UserManager<AppUser> userManager)
    {
        var customer = CustomerFactory.CreateCustomer().Value;
        var testAppUser = CreateTestAppUser();
        var labor = testAppUser.Employee!;
        var repairTask = RepairTaskFactory.CreateRepairTask().Value;
        var vehicleId = customer.Vehicles.FirstOrDefault()?.Id ?? Guid.CreateVersion7();

        var identityResult = await userManager.CreateAsync(testAppUser, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        var roleResult = await userManager.AddToRoleAsync(testAppUser, "Labor");
        roleResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.RepairTasks.AddAsync(repairTask);
        await context.SaveChangesAsync(default);

        var workOrder = WorkOrderTestDataBuilder.Create()
                    .WithTimeSlot(DateTimeOffset.UtcNow.AddMinutes(-3), DateTimeOffset.UtcNow.AddMinutes(30))
                    .WithRepairTasks(WorkOrderTaskFactory.CreateTask(originalTaskId: repairTask.Id))
                    .WithVehicle(vehicleId)
                    .WithLabor(labor.Id)
                    .Build();

        await context.WorkOrders.AddAsync(workOrder);
        await context.SaveChangesAsync(default);

        return (workOrder, testAppUser);
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