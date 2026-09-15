using FluentAssertions;

using MechanicShop.Application.Features.WorkOrders.Mappers;
using MechanicShop.Domain.Workorders;
using MechanicShop.Tests.Common.Billing;
using MechanicShop.Tests.Common.Customers;
using MechanicShop.Tests.Common.Employees;
using MechanicShop.Tests.Common.WorkOrders;

using Xunit;

namespace MechanicShop.Application.UnitTests.Mappers;

public class WorkOrderMapperTests
{
    [Fact]
    public void ToDto_ShouldMapCorrectly()
    {
        // Arrange
        var customer = CustomerFactory.CreateCustomer().Value;
        var labor = EmployeeFactory.CreateLabor().Value.Employee!;
        var vehicle = customer.Vehicles.First();

        var orderPart = WorkOrderTaskPartFactory.CreatePart(cost: 100m, quantity: 2);
        var orderTask = WorkOrderTaskFactory.CreateTask(laborCost: 150m, parts: [orderPart]);

        const decimal expectedPartCost = 200m; // 100 * 2
        const decimal expectedLaborCost = 150m;
        const decimal expectedTotalCost = 350m; // 200 + 150
        var expectedDuration = (int)orderTask.EstimatedDurationInMins;

        var workOrder = WorkOrderFactory.CreateTestWorkOrder(
            vehicleId: vehicle.Id,
            laborId: labor.Id,
            repairTasks: [orderTask]).Value;

        var invoiceLine = InvoiceLineItemFactory.CreateInvoiceLineItem(
            lineNumber: 1,
            description: "some description",
            quantity: 1,
            unitPrice: expectedTotalCost).Value;

        var invoice = InvoiceFactory.CreateInvoice(
            workOrderId: workOrder.Id,
            items: [invoiceLine]).Value;

        workOrder.Vehicle = vehicle;
        workOrder.Labor = labor;
        workOrder.Invoice = invoice;

        // Act
        var dto = workOrder.ToDto();

        // Assert
        dto.WorkOrderId.Should().Be(workOrder.Id);
        dto.Spot.Should().Be(workOrder.Spot);
        dto.StartAtUtc.Should().Be(workOrder.StartAtUtc);
        dto.EndAtUtc.Should().Be(workOrder.EndAtUtc);
        dto.State.Should().Be(workOrder.State);
        dto.CreatedAt.Should().Be(workOrder.CreatedAtUtc);

        dto.Labor.Should().NotBeNull();
        dto.Labor!.EmployeeId.Should().Be(workOrder.LaborId);
        dto.Labor.Name.Should().Be(labor.Name.FullName);

        dto.Vehicle.Should().NotBeNull();
        dto.Vehicle!.VehicleId.Should().Be(vehicle.Id);
        dto.Vehicle.Make.Should().Be(vehicle.Make);
        dto.Vehicle.Model.Should().Be(vehicle.Model);
        dto.Vehicle.Year.Should().Be(vehicle.Year);
        dto.Vehicle.LicensePlate.Should().Be(vehicle.LicensePlate);

        dto.RepairTasks.Should().ContainSingle();
        dto.TotalPartCost.Should().Be(expectedPartCost);
        dto.TotalLaborCost.Should().Be(expectedLaborCost);
        dto.TotalCost.Should().Be(expectedTotalCost);
        dto.TotalDurationInMins.Should().Be(expectedDuration);
        dto.InvoiceId.Should().Be(invoice.Id);
    }

    [Fact]
    public void ToDtos_ShouldMapListCorrectly()
    {
        // Arrange
        var customer = CustomerFactory.CreateCustomer().Value;
        var labor = EmployeeFactory.CreateLabor().Value.Employee!;
        var vehicle = customer.Vehicles.First();

        var orderPart = WorkOrderTaskPartFactory.CreatePart(cost: 100m, quantity: 2);
        var orderTask = WorkOrderTaskFactory.CreateTask(laborCost: 150m, parts: [orderPart]);

        var workOrder = WorkOrderFactory.CreateTestWorkOrder(
            vehicleId: vehicle.Id,
            laborId: labor.Id,
            repairTasks: [orderTask]).Value;

        workOrder.Vehicle = vehicle;
        workOrder.Labor = labor;

        var workOrders = new List<WorkOrder> { workOrder };

        // Act
        var dtos = workOrders.ToDtos();

        // Assert
        dtos.Should().ContainSingle();
        var dto = dtos[0];

        dto.WorkOrderId.Should().Be(workOrder.Id);
        dto.Spot.Should().Be(workOrder.Spot);
        dto.StartAtUtc.Should().Be(workOrder.StartAtUtc);
        dto.EndAtUtc.Should().Be(workOrder.EndAtUtc);

        dto.Labor.Should().NotBeNull();
        dto.Labor!.Name.Should().Be(labor.Name.FullName);
        dto.Labor.EmployeeId.Should().Be(labor.Id);

        dto.Vehicle.Should().NotBeNull();
        dto.RepairTasks.Should().ContainSingle();
        dto.State.Should().Be(workOrder.State);
    }

    [Fact]
    public void ToListItemDto_ShouldMapSummaryCorrectly()
    {
        // Arrange
        var customer = CustomerFactory.CreateCustomer().Value;
        var labor = EmployeeFactory.CreateLabor().Value.Employee!;
        var vehicle = customer.Vehicles.First();

        var orderPart = WorkOrderTaskPartFactory.CreatePart(cost: 100m, quantity: 2);
        var orderTask = WorkOrderTaskFactory.CreateTask(name: "Brake Replacement", laborCost: 150m, parts: [orderPart]);

        var workOrder = WorkOrderFactory.CreateTestWorkOrder(
            vehicleId: vehicle.Id,
            laborId: labor.Id,
            repairTasks: [orderTask]).Value;

        workOrder.Vehicle = vehicle;
        workOrder.Labor = labor;

        // Act
        var dto = workOrder.ToListItemDto();

        // Assert
        dto.WorkOrderId.Should().Be(workOrder.Id);
        dto.Spot.Should().Be(workOrder.Spot);
        dto.StartAtUtc.Should().Be(workOrder.StartAtUtc);
        dto.EndAtUtc.Should().Be(workOrder.EndAtUtc);

        dto.Vehicle.Should().NotBeNull();
        dto.Vehicle!.Make.Should().Be(vehicle.Make);
        dto.Labor.Should().Be(labor.Name.FullName);

        dto.RepairTasks.Should().ContainSingle().Which.Should().Be("Brake Replacement");
        dto.State.Should().Be(workOrder.State);
    }
}