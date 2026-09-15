using System.Net;
using System.Net.Http.Json;

using Docker.DotNet.Models;

using FluentAssertions;

using MechanicShop.Api.IntegrationTests.Common;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Api.Requests.Invoices;
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
public class InvoicesControllerTests : BaseIntegrationTest
{
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly WebAppFactory _factory;

    public InvoicesControllerTests(WebAppFactory factory)
        : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task IssueInvoice_WithValidWorkOrder_ShouldReturnCreatedInvoice()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        using var scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var workOrder = await SeedWorkOrderWithDependenciesAsync(context);

        // Transition through allowed states: Scheduled -> InProgress -> Completed
        await MakeWorkOrderCompleted(workOrder, context);

        var request = new IssueInvoiceRequest { DiscountAmount = 0M };

        var response = await Client.PostAsJsonAsync($"/api/v1.0/invoices/workorders/{workOrder.Id}", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var dto = await response.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.NotNull(dto);
        Assert.NotEqual(Guid.Empty, dto!.InvoiceId);
    }

    [Fact]
    public async Task GetInvoice_WithInvalidId_ShouldReturnNotFound()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        var response = await Client.GetAsync($"/api/v1.0/invoices/{Guid.CreateVersion7()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetInvoicePdf_WithValidInvoice_ShouldReturnPdf()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        // Arrange: create work order and issue invoice
        using var scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var workOrder = await SeedWorkOrderWithDependenciesAsync(context);

        // Ensure the work order is completed before issuing an invoice
        await MakeWorkOrderCompleted(workOrder, context);

        var issueRequest = new IssueInvoiceRequest { DiscountAmount = 0M };
        var issueResponse = await Client.PostAsJsonAsync($"/api/v1.0/invoices/workorders/{workOrder.Id}", issueRequest);
        issueResponse.EnsureSuccessStatusCode();
        var invoice = await issueResponse.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.NotNull(invoice);

        // Act: get pdf
        var pdfResponse = await Client.GetAsync($"/api/v1.0/invoices/{invoice!.InvoiceId}/pdf");

        Assert.Equal(HttpStatusCode.OK, pdfResponse.StatusCode);
        Assert.Equal("application/pdf", pdfResponse.Content.Headers.ContentType?.MediaType);
        var bytes = await pdfResponse.Content.ReadAsByteArrayAsync();
        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task SettleInvoice_WithValidInvoice_ShouldReturnNoContent()
    {
        var token = await Client.GenerateTokenAsync(TestUsers.Manager);
        Client.SetAuthorizationHeader(token);

        // Arrange: create work order and issue invoice
        using var scope = Factory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var workOrder = await SeedWorkOrderWithDependenciesAsync(context);

        // Ensure the work order is completed so invoice can be issued
        await MakeWorkOrderCompleted(workOrder, context);

        var issueRequest = new IssueInvoiceRequest { DiscountAmount = 0M };
        var issueResponse = await Client.PostAsJsonAsync($"/api/v1.0/invoices/workorders/{workOrder.Id}", issueRequest);
        issueResponse.EnsureSuccessStatusCode();
        var invoice = await issueResponse.Content.ReadFromJsonAsync<InvoiceDto>();
        Assert.NotNull(invoice);

        // Act: settle
        var settleResponse = await Client.PutAsJsonAsync($"/api/v1.0/invoices/{invoice!.InvoiceId}/payments", new { });

        Assert.Equal(HttpStatusCode.NoContent, settleResponse.StatusCode);
    }

    private async Task<WorkOrder> SeedWorkOrderWithDependenciesAsync(IAppDbContext context)
    {
        using var scope = _factory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customer = CustomerFactory.CreateCustomer().Value;
        var testAppUser = CreateTestAppUser();
        var repairTask = RepairTaskFactory.CreateRepairTask().Value;
        var vehicleId = customer.Vehicles.FirstOrDefault()?.Id ?? Guid.CreateVersion7();
        var labor = testAppUser.Employee!;

        var identityResult = await userManager.CreateAsync(testAppUser, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

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

    private async Task<WorkOrder> MakeWorkOrderCompleted(WorkOrder wo, IAppDbContext context)
    {
        wo.UpdateState(Domain.Workorders.Enums.WorkOrderState.InProgress, _timeProvider);
        wo.UpdateState(Domain.Workorders.Enums.WorkOrderState.Completed, _timeProvider);
        await context.SaveChangesAsync(default);
        return wo;
    }
}