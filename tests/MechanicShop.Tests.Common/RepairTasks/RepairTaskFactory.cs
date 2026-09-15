using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.RepairTasks;
using MechanicShop.Domain.RepairTasks.Enums;

namespace MechanicShop.Tests.Common.RepairTasks;

public static class RepairTaskFactory
{
    public static Result<RepairTask> CreateRepairTask(
        Guid? id = null,
        string? name = null,
        decimal? laborCost = null,
        RepairDurationInMinutes? repairDurationInMinutes = null,
        List<RepairTaskPartData>? parts = null)
    {
        return RepairTask.Create(
            id ?? Guid.CreateVersion7(),
            name ?? "Brake Inspection",
            laborCost ?? 100,
            repairDurationInMinutes ?? RepairDurationInMinutes.Min30,
            parts ?? new List<RepairTaskPartData> { new RepairTaskPartData(Guid.CreateVersion7(), 4) });
    }
}