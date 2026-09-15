using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.Queries.GetWorkOrders;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.Customers.Vehicles;
using MechanicShop.Domain.Workorders.Enums;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Tests.Common.Customers;
using MechanicShop.Tests.Common.Employees;
using MechanicShop.Tests.Common.WorkOrders;

using MediatR;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.WorkOrders.Queries.GetWorkOrders;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetWorkOrdersQueryTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WithNoFilters_ShouldReturnAllWorkOrdersPaginated()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customerId = Guid.CreateVersion7();
        var vehicle = VehicleFactory.CreateVehicle(make: "Toyota", licensePlate: ShortLicense("ABC"), customerId: customerId).Value;
        var customer = CustomerFactory.CreateCustomer(vehicles: new List<Vehicle> { vehicle }, id: customerId).Value;

        var appUserWithEmployee = EmployeeFactory.CreateLabor().Value;
        var employee = appUserWithEmployee.Employee!;

        var workOrder1 = WorkOrderFactory.CreateTestWorkOrder(vehicleId: vehicle.Id, laborId: employee.Id).Value;
        var workOrder2 = WorkOrderFactory.CreateTestWorkOrder(vehicleId: vehicle.Id, laborId: employee.Id).Value;

        var identityResult = await userManager.CreateAsync(appUserWithEmployee, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        await context.WorkOrders.AddRangeAsync(workOrder1, workOrder2);
        await context.SaveChangesAsync(default);

        var query = new GetPublicTrackingInfoQuery(Page: 1, PageSize: 10, SearchTerm: null);

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var paginatedList = result.Value;
        paginatedList.Items.Should().HaveCountGreaterOrEqualTo(2);
        paginatedList.TotalCount.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldReturnMatchingWorkOrders()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customerId = Guid.CreateVersion7();
        var toyotaVehicle = VehicleFactory.CreateVehicle(make: "Toyota", licensePlate: ShortLicense("ABC"), customerId: customerId).Value;
        var hondaVehicle = VehicleFactory.CreateVehicle(make: "Honda", licensePlate: ShortLicense("XYZ"), customerId: customerId).Value;

        var customer = CustomerFactory.CreateCustomer(vehicles: new List<Vehicle> { toyotaVehicle, hondaVehicle }, id: customerId).Value;

        var appUserWithEmployee = EmployeeFactory.CreateLabor().Value;
        var employee = appUserWithEmployee.Employee!;

        var workOrderToyota = WorkOrderFactory.CreateTestWorkOrder(id: Guid.CreateVersion7(), vehicleId: toyotaVehicle.Id, laborId: employee.Id, spot: Spot.A).Value;
        var workOrderHonda = WorkOrderFactory.CreateTestWorkOrder(id: Guid.CreateVersion7(), vehicleId: hondaVehicle.Id, laborId: employee.Id, spot: Spot.B).Value;

        var identityResult = await userManager.CreateAsync(appUserWithEmployee, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        await context.WorkOrders.AddRangeAsync(workOrderToyota, workOrderHonda);
        await context.SaveChangesAsync(default);

        var query = new GetPublicTrackingInfoQuery(Page: 1, PageSize: 10, SearchTerm: toyotaVehicle.LicensePlate);

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var paginatedList = result.Value;
        paginatedList.Items.Should().ContainSingle();
        var item = paginatedList.Items!.First();
        item.WorkOrderId.Should().Be(workOrderToyota.Id);
        item.Vehicle!.Make.Should().Be("Toyota");
        item.Vehicle.LicensePlate.Should().Be(toyotaVehicle.LicensePlate);
    }

    [Fact]
    public async Task Handle_WithStateFilter_ShouldReturnOnlyMatchingStates()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customerId = Guid.CreateVersion7();
        var vehicle = VehicleFactory.CreateVehicle(make: "Ford", licensePlate: ShortLicense("PL"), customerId: customerId).Value;
        var customer = CustomerFactory.CreateCustomer(vehicles: new List<Vehicle> { vehicle }, id: customerId).Value;

        var appUserWithEmployee = EmployeeFactory.CreateLabor().Value;
        var employee = appUserWithEmployee.Employee!;

        var scheduledWorkOrder = WorkOrderFactory.CreateTestWorkOrder(id: Guid.CreateVersion7(), vehicleId: vehicle.Id, laborId: employee.Id).Value;
        var inProgressWorkOrder = WorkOrderFactory.CreateTestWorkOrder(id: Guid.CreateVersion7(), vehicleId: vehicle.Id, laborId: employee.Id).Value;

        var startAt = DateTimeOffset.UtcNow.AddHours(-1);
        var endAt = startAt.AddHours(2);
        inProgressWorkOrder.UpdateTiming(startAt, endAt);
        inProgressWorkOrder.UpdateState(WorkOrderState.InProgress, scope.ServiceProvider.GetRequiredService<TimeProvider>());

        var identityResult = await userManager.CreateAsync(appUserWithEmployee, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        await context.WorkOrders.AddRangeAsync(scheduledWorkOrder, inProgressWorkOrder);
        await context.SaveChangesAsync(default);

        var query = new GetPublicTrackingInfoQuery(Page: 1, PageSize: 10, SearchTerm: null, State: WorkOrderState.InProgress);

        // Act
        var result = await mediator.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var paginatedList = result.Value;
        paginatedList.Items.Should().ContainSingle();
        paginatedList.Items?.First().WorkOrderId.Should().Be(inProgressWorkOrder.Id);
        paginatedList.Items?.First().State.Should().Be(WorkOrderState.InProgress);
    }

    [Fact]
    public async Task Handle_WithSorting_ShouldSortItemsCorrectly()
    {
        // Arrange
        using var scope = _factory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var customerId = Guid.CreateVersion7();
        var vehicle = VehicleFactory.CreateVehicle(make: "Nissan", licensePlate: ShortLicense("ABC"), customerId: customerId).Value;
        var customer = CustomerFactory.CreateCustomer(vehicles: new List<Vehicle> { vehicle }, id: customerId).Value;

        var appUserWithEmployee = EmployeeFactory.CreateLabor().Value;
        var employee = appUserWithEmployee.Employee!;

        var baseTime = DateTimeOffset.UtcNow.Date.AddDays(1);

        var earlyWorkOrder = WorkOrderFactory.CreateTestWorkOrder(
            id: Guid.CreateVersion7(),
            vehicleId: vehicle.Id,
            laborId: employee.Id,
            startAt: baseTime.AddHours(10),
            endAt: baseTime.AddHours(11)).Value;

        var lateWorkOrder = WorkOrderFactory.CreateTestWorkOrder(
            id: Guid.CreateVersion7(),
            vehicleId: vehicle.Id,
            laborId: employee.Id,
            startAt: baseTime.AddHours(15),
            endAt: baseTime.AddHours(16)).Value;

        var identityResult = await userManager.CreateAsync(appUserWithEmployee, "SecurePassword123!");
        identityResult.Succeeded.Should().BeTrue();

        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync(default);

        await context.WorkOrders.AddRangeAsync(earlyWorkOrder, lateWorkOrder);
        await context.SaveChangesAsync(default);

        var queryAsc = new GetPublicTrackingInfoQuery(
            Page: 1,
            PageSize: 10,
            SearchTerm: null,
            SortColumn: "startat",
            SortDirection: "asc");

        // Act
        var result = await mediator.Send(queryAsc);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var paginatedList = result.Value;

        var filteredItems = paginatedList.Items!
            .Where(x => x.WorkOrderId == earlyWorkOrder.Id || x.WorkOrderId == lateWorkOrder.Id)
            .ToList();

        filteredItems.Should().HaveCount(2);
        filteredItems[0].WorkOrderId.Should().Be(earlyWorkOrder.Id);
        filteredItems[1].WorkOrderId.Should().Be(lateWorkOrder.Id);
    }

    private static string ShortLicense(string prefix, int maxLen = 12)
    {
        var id = Guid.CreateVersion7().ToString("N");
        var combined = $"{prefix}-{id}";
        return combined.Length <= maxLen ? combined : combined.Substring(0, maxLen);
    }
}