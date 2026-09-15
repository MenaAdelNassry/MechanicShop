using MechanicShop.Domain.RepairTasks.Enums;

namespace MechanicShop.Domain.Workorders;

public sealed class WorkOrderTask
{
    public Guid Id { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public Guid OriginalRepairTaskId { get; private set; }
    public string Name { get; private set; } = null!;
    public decimal LaborCost { get; private set; }
    public RepairDurationInMinutes EstimatedDurationInMins { get; private set; }

    public decimal PartsCost => _parts.Sum(p => p.Cost * p.Quantity);
    public decimal TotalCost => LaborCost + PartsCost;

    private readonly List<WorkOrderTaskPart> _parts = [];
    public IReadOnlyCollection<WorkOrderTaskPart> Parts => _parts.AsReadOnly();

    public WorkOrderTask(
        Guid id,
        Guid originalTaskId,
        string name,
        decimal laborCost,
        RepairDurationInMinutes estimatedDurationInMins,
        List<WorkOrderTaskPart> parts)
    {
        Id = id == Guid.Empty ? Guid.CreateVersion7() : id;
        OriginalRepairTaskId = originalTaskId;
        Name = name.Trim();
        LaborCost = laborCost;
        EstimatedDurationInMins = estimatedDurationInMins;
        _parts = parts ?? [];
    }

#pragma warning disable CS8618
    private WorkOrderTask() { }
#pragma warning restore CS8618

}