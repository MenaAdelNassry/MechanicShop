namespace MechanicShop.Application.Features.RepairTasks.Commands.UpdateRepairTask;

public sealed record UpdateRepairTaskPartCommand(
    Guid InventoryItemId,
    int Quantity
);