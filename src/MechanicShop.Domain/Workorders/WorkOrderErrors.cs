using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Enums;

namespace MechanicShop.Domain.Workorders;

public static class WorkOrderErrors
{
    public static readonly Error WorkOrderIdRequired =
        Error.Validation("WorkOrder.Id.Required", "WorkOrder Id is required.");

    public static readonly Error SpotIdRequired =
        Error.Validation("WorkOrder.SpotId.Required", "Spot Id is required.");

    public static readonly Error VehicleIdRequired =
        Error.Validation("WorkOrder.VehicleId.Required", "Vehicle Id is required.");

    public static readonly Error RepairTasksRequired =
        Error.Validation("WorkOrder.RepairTasks.Required", "At least one repair task is required.");

    public static readonly Error LaborIdRequired =
        Error.Validation("WorkOrder.LaborId.Required", "Labor Id is required.");

    public static readonly Error InvalidTiming =
        Error.Conflict("WorkOrder.Timing.Invalid", "End time must be strictly after start time.");

    public static readonly Error SpotInvalid =
        Error.Validation("WorkOrder.Spot.Invalid", "The provided workshop spot is invalid.");

    public static readonly Error Readonly =
        Error.Conflict("WorkOrder.Readonly", "WorkOrder cannot be modified in its current state.");

    public static readonly Error RepairTaskAlreadyAdded =
        Error.Conflict("WorkOrder.RepairTask.Duplicate", "This repair task already exists in the work order.");

    public static Error CannotStartBeforeScheduledTime(DateTimeOffset startAtUtc) =>
        Error.Conflict("WorkOrder.Start.TooEarly", $"Cannot start work order before its scheduled start time: {startAtUtc:yyyy-MM-dd HH:mm} UTC.");

    public static Error TimingReadonly(Guid id, WorkOrderState state) =>
        Error.Conflict("WorkOrder.Timing.Readonly", $"WorkOrder '{id}': Cannot modify schedule when status is '{state}'.");

    public static Error InvalidStateTransition(WorkOrderState current, WorkOrderState next) =>
        Error.Conflict("WorkOrder.State.InvalidTransition", $"Invalid state transition from '{current}' to '{next}'.");
}