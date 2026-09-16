using System;

using MechanicShop.Domain.Common;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Customers.Vehicles;
using MechanicShop.Domain.Employees;
using MechanicShop.Domain.Spots;
using MechanicShop.Domain.Workorders.Billing;
using MechanicShop.Domain.Workorders.Enums;
using MechanicShop.Domain.Workorders.Events;

namespace MechanicShop.Domain.Workorders;

public sealed class WorkOrder : AuditableEntity
{
    public Guid VehicleId { get; private set; }
    public Guid SpotId { get; private set; }
    public DateTimeOffset StartAtUtc { get; private set; }
    public DateTimeOffset EndAtUtc { get; private set; }
    public Guid LaborId { get; private set; }
    public WorkOrderState State { get; private set; }
    public Guid TrackingToken { get; private set; }
    public DateTimeOffset? ActualStartedAtUtc { get; private set; }
    public DateTimeOffset? ActualCompletedAtUtc { get; private set; }

    // Navigation properties
    public Employee? Labor { get; internal set; }
    public Vehicle? Vehicle { get; internal set; }
    public Invoice? Invoice { get; internal set; }
    public ServiceBay? Spot { get; private set; }

    // Computed properties
    public decimal TotalPartsCost => _repairTasks.SelectMany(rt => rt.Parts).Sum(p => p.Cost * p.Quantity);
    public decimal TotalLaborCost => _repairTasks.Sum(rt => rt.LaborCost);
    public decimal Total => TotalPartsCost + TotalLaborCost;

    public TimeSpan? ActualDuration =>
        (ActualStartedAtUtc.HasValue && ActualCompletedAtUtc.HasValue)
            ? ActualCompletedAtUtc.Value - ActualStartedAtUtc.Value
            : null;

    private readonly List<WorkOrderTask> _repairTasks = [];
    public IReadOnlyCollection<WorkOrderTask> RepairTasks => _repairTasks.AsReadOnly();

    public bool IsEditable => State is WorkOrderState.Scheduled;

#pragma warning disable CS8618
    private WorkOrder() { }
#pragma warning restore CS8618

    private WorkOrder(
        Guid id,
        Guid vehicleId,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        Guid laborId,
        Guid spotId,
        WorkOrderState state,
        List<WorkOrderTask> repairTasks,
        Guid trackingToken)
        : base(id)
    {
        VehicleId = vehicleId;
        StartAtUtc = startAt;
        EndAtUtc = endAt;
        LaborId = laborId;
        SpotId = spotId;
        State = state;
        _repairTasks = repairTasks;
        TrackingToken = trackingToken;
    }

    public static Result<WorkOrder> Create(
        Guid id,
        Guid vehicleId,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        Guid laborId,
        Guid spotId,
        List<WorkOrderTask> repairTasks)
    {
        if (id == Guid.Empty) return WorkOrderErrors.WorkOrderIdRequired;
        if (vehicleId == Guid.Empty) return WorkOrderErrors.VehicleIdRequired;
        if (laborId == Guid.Empty) return WorkOrderErrors.LaborIdRequired;
        if (spotId == Guid.Empty) return WorkOrderErrors.SpotIdRequired;
        if (repairTasks is null || repairTasks.Count == 0) return WorkOrderErrors.RepairTasksRequired;
        if (endAt <= startAt) return WorkOrderErrors.InvalidTiming;

        var trackingToken = Guid.CreateVersion7();
        var workOrder = new WorkOrder(id, vehicleId, startAt, endAt, laborId, spotId, WorkOrderState.Scheduled, repairTasks, trackingToken);

        workOrder.AddDomainEvent(new WorkOrderCreated(workOrder.Id, trackingToken));
        workOrder.AddDomainEvent(new WorkOrderCollectionModified());

        return workOrder;
    }

    public Result<Updated> UpdateState(WorkOrderState newState, TimeProvider timeProvider)
    {
        if (!CanTransitionTo(newState))
        {
            return WorkOrderErrors.InvalidStateTransition(State, newState);
        }

        var now = timeProvider.GetUtcNow();

        if (newState == WorkOrderState.InProgress && StartAtUtc > now)
        {
            return WorkOrderErrors.CannotStartBeforeScheduledTime(StartAtUtc);
        }

        State = newState;

        if (State == WorkOrderState.InProgress && ActualStartedAtUtc is null)
        {
            ActualStartedAtUtc = now;
        }
        else if (State == WorkOrderState.Completed)
        {
            ActualCompletedAtUtc = now;
            AddDomainEvent(new WorkOrderCompleted(Id));
        }
        else if (State == WorkOrderState.Cancelled)
        {
            var partsData = RepairTasks
                .SelectMany(t => t.Parts.Select(p => new ReservedPartData(p.InventoryItemId, p.Quantity, t.Id)))
                .ToList();
            AddDomainEvent(new WorkOrderCancelled(Id, partsData));
        }

        AddDomainEvent(new WorkOrderCollectionModified(TrackingToken, State));

        return Result.Updated;
    }

    public Result<Updated> Cancel(TimeProvider timeProvider)
    {
        return UpdateState(WorkOrderState.Cancelled, timeProvider);
    }

    public bool CanTransitionTo(WorkOrderState newStatus)
    {
        return (State, newStatus) switch
        {
            (WorkOrderState.Scheduled, WorkOrderState.InProgress) => true,
            (WorkOrderState.InProgress, WorkOrderState.Completed) => true,
            (WorkOrderState.Scheduled, WorkOrderState.Cancelled) => true,
            _ => false
        };
    }

    public Result<Updated> UpdateTiming(DateTimeOffset startAt, DateTimeOffset endAt)
    {
        if (!IsEditable)
        {
            return WorkOrderErrors.TimingReadonly(Id, State);
        }

        if (endAt <= startAt)
        {
            return WorkOrderErrors.InvalidTiming;
        }

        StartAtUtc = startAt;
        EndAtUtc = endAt;

        AddDomainEvent(new WorkOrderCollectionModified());

        return Result.Updated;
    }

    public Result<Updated> UpdateLabor(Guid laborId)
    {
        if (!IsEditable) return WorkOrderErrors.Readonly;
        if (laborId == Guid.Empty) return WorkOrderErrors.LaborIdRequired;

        LaborId = laborId;
        AddDomainEvent(new WorkOrderCollectionModified());
        return Result.Updated;
    }

    public Result<Updated> UpdateSpot(Guid spotId)
    {
        if (!IsEditable) return WorkOrderErrors.Readonly;
        if (spotId == Guid.Empty) return WorkOrderErrors.SpotIdRequired;

        SpotId = spotId;
        AddDomainEvent(new WorkOrderCollectionModified());
        return Result.Updated;
    }

    public Result<Updated> AddRepairTask(WorkOrderTask repairTask)
    {
        if (!IsEditable)
        {
            return WorkOrderErrors.Readonly;
        }

        if (repairTask is null)
        {
            return WorkOrderErrors.RepairTasksRequired;
        }

        if (_repairTasks.Any(r => r.Id == repairTask.Id || r.OriginalRepairTaskId == repairTask.OriginalRepairTaskId))
        {
            return WorkOrderErrors.RepairTaskAlreadyAdded;
        }

        _repairTasks.Add(repairTask);

        AddDomainEvent(new WorkOrderCollectionModified());

        return Result.Updated;
    }

    public Result<Updated> ClearRepairTasks()
    {
        if (!IsEditable)
        {
            return WorkOrderErrors.Readonly;
        }

        _repairTasks.Clear();

        AddDomainEvent(new WorkOrderCollectionModified());

        return Result.Updated;
    }
}