using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Domain.RepairTasks;

public static class RepairTaskErrors
{
    public static readonly Error IdRequired =
        Error.Validation("RepairTask.Id.Required", "Repair task ID is required.");

    public static readonly Error PartRepairTaskIdRequired =
        Error.Validation("RepairTask.Part.RepairTaskIdRequired", "Repair task ID is required for the part.");

    public static readonly Error NameRequired =
        Error.Validation("RepairTask.Name.Required", "Name is required.");

    public static readonly Error LaborCostInvalid =
        Error.Validation("RepairTask.LaborCost.Invalid", "Labor cost must be between 1 and 10,000.");

    public static readonly Error DurationInvalid =
        Error.Validation("RepairTask.Duration.Invalid", "Invalid duration selected.");

    public static readonly Error DuplicatePart =
        Error.Conflict("RepairTask.Parts.Duplicate", "Duplicate inventory parts found in the same repair task.");

    public static readonly Error AlreadyDeleted =
        Error.Conflict("RepairTask.AlreadyDeleted", "Repair task is already deleted.");

    public static readonly Error AlreadyActive =
        Error.Conflict("RepairTask.AlreadyActive", "Repair task is already active.");

    public static readonly Error PartInventoryItemIdRequired =
    Error.Validation("RepairTask.Part.InventoryItemIdRequired", "InventoryItemId is required.");

    public static readonly Error PartQuantityInvalid =
        Error.Validation("RepairTask.Part.QuantityInvalid", "Quantity must be greater than zero.");
}