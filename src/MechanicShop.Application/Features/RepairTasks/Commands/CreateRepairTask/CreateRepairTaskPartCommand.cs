namespace MechanicShop.Application.Features.RepairTasks.Commands.CreateRepairTask;

public sealed record CreateRepairTaskPartCommand(
    Guid InventoryItemId,
    int Quantity
);