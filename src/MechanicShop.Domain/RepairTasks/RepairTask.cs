using System.Runtime.Intrinsics.Arm;

using MechanicShop.Domain.Common;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.RepairTasks.Enums;

namespace MechanicShop.Domain.RepairTasks;

public sealed class RepairTask : AuditableEntity, ISoftDelete
{
    public string Name { get; private set; } = null!;
    public decimal LaborCost { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public RepairDurationInMinutes EstimatedDurationInMins { get; private set; }

    private readonly List<RepairTaskPart> _parts = [];
    public IReadOnlyCollection<RepairTaskPart> Parts => _parts.AsReadOnly();

#pragma warning disable CS8618
    private RepairTask() { }
#pragma warning restore CS8618

    private RepairTask(
        Guid id,
        string name,
        decimal laborCost,
        RepairDurationInMinutes estimatedDurationInMins,
        List<RepairTaskPart> parts)
        : base(id)
    {
        Name = name;
        LaborCost = laborCost;
        EstimatedDurationInMins = estimatedDurationInMins;
        _parts = parts;
    }

    public static Result<RepairTask> Create(
        Guid id,
        string? name,
        decimal laborCost,
        RepairDurationInMinutes estimatedDurationInMins,
        IEnumerable<RepairTaskPartData>? parts = null)
    {
        if (id == Guid.Empty)
            return RepairTaskErrors.IdRequired;

        var validationResult = Validate(name, laborCost, estimatedDurationInMins);
        if (validationResult.IsError)
            return validationResult.Errors;

        var partsList = new List<RepairTaskPart>();
        if (parts is not null)
        {
            var seenItemIds = new HashSet<Guid>();

            foreach (var p in parts)
            {
                if (!seenItemIds.Add(p.InventoryItemId))
                    return RepairTaskErrors.DuplicatePart;

                var partResult = RepairTaskPart.Create(id, p.InventoryItemId, p.Quantity);
                if (partResult.IsError)
                    return partResult.Errors;

                partsList.Add(partResult.Value);
            }
        }

        return new RepairTask(id, name!.Trim(), laborCost, estimatedDurationInMins, partsList);
    }

    public Result<Updated> Update(string? name, decimal laborCost, RepairDurationInMinutes estimatedDurationInMins)
    {
        var validationResult = Validate(name, laborCost, estimatedDurationInMins);
        if (validationResult.IsError)
            return validationResult.Errors;

        Name = name!.Trim();
        LaborCost = laborCost;
        EstimatedDurationInMins = estimatedDurationInMins;

        return Result.Updated;
    }

    public Result<Updated> UpsertParts(IEnumerable<RepairTaskPartData> incomingParts)
    {
        var incomingList = incomingParts.ToList();
        var incomingIds = incomingList.Select(p => p.InventoryItemId).ToHashSet();

        // 1. Delete items not found in the new list
        _parts.RemoveAll(existing => !incomingIds.Contains(existing.InventoryItemId));

        // 2. Update existing or add new
        foreach (var incoming in incomingList)
        {
            var existing = _parts.FirstOrDefault(p => p.InventoryItemId == incoming.InventoryItemId);
            if (existing is null)
            {
                var createResult = RepairTaskPart.Create(Id, incoming.InventoryItemId, incoming.Quantity);
                if (createResult.IsError)
                    return createResult.Errors;

                _parts.Add(createResult.Value);
            }
            else
            {
                var updateResult = existing.UpdateQuantity(incoming.Quantity);
                if (updateResult.IsError)
                    return updateResult.Errors;
            }
        }

        return Result.Updated;
    }

    public Result<Updated> Delete(TimeProvider? timeProvider = null)
    {
        if (IsDeleted)
            return RepairTaskErrors.AlreadyDeleted;

        IsDeleted = true;
        DeletedAtUtc = timeProvider?.GetUtcNow() ?? DateTimeOffset.UtcNow;

        return Result.Updated;
    }

    public Result<Updated> Activate()
    {
        if (!IsDeleted)
            return RepairTaskErrors.AlreadyActive;

        IsDeleted = false;
        DeletedAtUtc = null;

        return Result.Updated;
    }

    private static Result<Updated> Validate(string? name, decimal laborCost, RepairDurationInMinutes estimatedDurationInMins)
    {
        if (string.IsNullOrWhiteSpace(name))
            return RepairTaskErrors.NameRequired;

        if (laborCost <= 0 || laborCost > 10_000)
            return RepairTaskErrors.LaborCostInvalid;

        if (!Enum.IsDefined(estimatedDurationInMins))
            return RepairTaskErrors.DurationInvalid;

        return Result.Updated;
    }
}