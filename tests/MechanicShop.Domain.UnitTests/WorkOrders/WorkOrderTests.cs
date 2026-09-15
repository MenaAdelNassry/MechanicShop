using FluentAssertions;

using MechanicShop.Domain.Workorders;
using MechanicShop.Domain.Workorders.Enums;
using MechanicShop.Tests.Common;
using MechanicShop.Tests.Common.WorkOrders;

using Xunit;

namespace MechanicShop.Domain.UnitTests.WorkOrders;

public class WorkOrderTests
{
    [Fact]
    public void Create_ShouldReturnError_WhenIdIsEmpty()
    {
        // Act & Assert
        var wo = WorkOrderFactory.CreateTestWorkOrder(id: Guid.Empty);

        wo.IsSuccess.Should().BeFalse();
        wo.TopError.Code.Should().Be(WorkOrderErrors.WorkOrderIdRequired.Code);
    }

    [Fact]
    public void Create_ShouldReturnError_WhenVehicleIdIsEmpty()
    {
        // Act & Assert
        var wo = WorkOrderFactory.CreateTestWorkOrder(vehicleId: Guid.Empty);

        wo.IsSuccess.Should().BeFalse();
        wo.TopError.Code.Should().Be(WorkOrderErrors.VehicleIdRequired.Code);
    }

    [Fact]
    public void Create_ShouldReturnError_WhenNoRepairTasks()
    {
        // Act & Assert
        var wo = WorkOrderFactory.CreateTestWorkOrder(repairTasks: []);

        wo.IsSuccess.Should().BeFalse();
        wo.TopError.Code.Should().Be(WorkOrderErrors.RepairTasksRequired.Code);
    }

    [Fact]
    public void Create_ShouldReturnError_WhenLaborIdIsEmpty()
    {
        // Act & Assert
        var wo = WorkOrderFactory.CreateTestWorkOrder(laborId: Guid.Empty);

        wo.IsSuccess.Should().BeFalse();
        wo.TopError.Code.Should().Be(WorkOrderErrors.LaborIdRequired.Code);
    }

    [Fact]
    public void Create_ShouldReturnError_WhenTimingInvalid()
    {
        // Act & Assert
        var now = DateTimeOffset.UtcNow;
        var wo = WorkOrderFactory.CreateTestWorkOrder(startAt: now.AddHours(1), endAt: now);

        wo.IsSuccess.Should().BeFalse();
        wo.TopError.Code.Should().Be(WorkOrderErrors.InvalidTiming.Code);
    }

    [Fact]
    public void Create_ShouldReturnError_WhenSpotInvalid()
    {
        // Act & Assert
        const Spot invalidSpot = (Spot)999;
        var wo = WorkOrderFactory.CreateTestWorkOrder(spot: invalidSpot);

        wo.IsSuccess.Should().BeFalse();
        wo.TopError.Code.Should().Be(WorkOrderErrors.SpotInvalid.Code);
    }

    [Fact]
    public void AddRepairTask_ShouldReturnError_WhenNotEditable()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(now);

        WorkOrder wo = WorkOrderFactory.CreateTestWorkOrder(startAt: now, endAt: now.AddHours(1)).Value;

        // Transition the aggregate to a closed state
        wo.UpdateState(WorkOrderState.InProgress, fakeTime);
        wo.UpdateState(WorkOrderState.Completed, fakeTime);

        // Act
        var result = wo.AddRepairTask(WorkOrderTaskFactory.CreateTask());

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.TopError.Code.Should().Be(WorkOrderErrors.Readonly.Code);
    }

    [Fact]
    public void UpdateLabor_ShouldReturnError_WhenLaborIdEmpty()
    {
        // Arrange
        WorkOrder wo = WorkOrderFactory.CreateTestWorkOrder().Value;

        // Act
        var result = wo.UpdateLabor(Guid.Empty);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.TopError.Code.Should().Be(WorkOrderErrors.LaborIdEmpty(wo.Id.ToString()).Code);
    }

    [Fact]
    public void UpdateSpot_ShouldReturnError_WhenSpotInvalid()
    {
        // Arrange
        WorkOrder wo = WorkOrderFactory.CreateTestWorkOrder().Value;
        const Spot invalidSpot = (Spot)999;

        // Act
        var result = wo.UpdateSpot(invalidSpot);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.TopError.Code.Should().Be(WorkOrderErrors.SpotInvalid.Code);
    }

    [Fact]
    public void UpdateTiming_ShouldReturnError_WhenInvalid()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        WorkOrder wo = WorkOrderFactory.CreateTestWorkOrder(startAt: now, endAt: now.AddHours(1)).Value;

        // Act
        var result = wo.UpdateTiming(now.AddHours(2), now);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.TopError.Code.Should().Be(WorkOrderErrors.InvalidTiming.Code);
    }

    [Fact]
    public void UpdateState_ShouldReturnError_WhenTransitionInvalid()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(now);

        WorkOrder wo = WorkOrderFactory.CreateTestWorkOrder(startAt: now, endAt: now.AddHours(1)).Value;

        // Act
        var result = wo.UpdateState(WorkOrderState.Completed, fakeTime);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.TopError.Code.Should().Be(WorkOrderErrors.InvalidStateTransition(WorkOrderState.Scheduled, WorkOrderState.Completed).Code);
    }

    [Fact]
    public void UpdateLabor_ShouldReturnSuccess_AndSetNewLaborId()
    {
        // Arrange
        WorkOrder wo = WorkOrderFactory.CreateTestWorkOrder().Value;
        Guid newLabor = Guid.CreateVersion7();

        // Act
        var result = wo.UpdateLabor(newLabor);

        // Assert
        result.IsSuccess.Should().BeTrue();
        wo.LaborId.Should().Be(newLabor);
    }

    [Fact]
    public void UpdateSpot_ShouldReturnSuccess_AndSetNewSpot()
    {
        // Arrange
        WorkOrder wo = WorkOrderFactory.CreateTestWorkOrder().Value;

        // Act
        var result = wo.UpdateSpot(Spot.B);

        // Assert
        result.IsSuccess.Should().BeTrue();
        wo.Spot.Should().Be(Spot.B);
    }

    [Fact]
    public void UpdateTiming_ShouldReturnSuccess_AndSetNewTiming()
    {
        // Arrange
        WorkOrder wo = WorkOrderFactory.CreateTestWorkOrder().Value;
        DateTimeOffset newStart = wo.StartAtUtc.AddHours(2);
        DateTimeOffset newEnd = newStart.AddHours(1);

        // Act
        var result = wo.UpdateTiming(newStart, newEnd);

        // Assert
        result.IsSuccess.Should().BeTrue();
        wo.StartAtUtc.Should().Be(newStart);
        wo.EndAtUtc.Should().Be(newEnd);
    }

    [Fact]
    public void UpdateState_ShouldReturnSuccess_AndSetStateToInProgress()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(now);

        WorkOrder wo = WorkOrderFactory.CreateTestWorkOrder(startAt: now, endAt: now.AddHours(1)).Value;

        // Act
        var result = wo.UpdateState(WorkOrderState.InProgress, fakeTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        wo.State.Should().Be(WorkOrderState.InProgress);
    }
}