using MechanicShop.Application.Features.RepairTasks.Dtos;
using MechanicShop.Domain.Inventory;
using MechanicShop.Domain.RepairTasks;
using MechanicShop.Domain.Workorders;

namespace MechanicShop.Application.Features.RepairTasks.Mappers;

public static class RepairTaskMapper
{
    // Mapper for Catalog RepairTask (Needs InventoryItems map to get Name & Cost)
    public static RepairTaskDto ToDto(this RepairTask entity, IReadOnlyDictionary<Guid, InventoryItem> inventoryItemsMap)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var partsDto = entity.Parts.Select(p =>
        {
            var item = inventoryItemsMap.GetValueOrDefault(p.InventoryItemId);
            return new RepairTaskPartDto
            {
                InventoryItemId = p.InventoryItemId,
                Quantity = p.Quantity,
                Name = item?.Name ?? string.Empty,
                Cost = item?.Cost ?? 0m
            };
        }).ToList();

        var partsTotalCost = partsDto.Sum(p => p.Cost * p.Quantity);

        return new RepairTaskDto
        {
            OriginalRepairTaskId = entity.Id,
            Name = entity.Name,
            LaborCost = entity.LaborCost,
            TotalCost = entity.LaborCost + partsTotalCost,
            EstimatedDurationInMins = entity.EstimatedDurationInMins,
            DeletedAtUtc = entity.IsDeleted ? entity.DeletedAtUtc : null,
            Parts = partsDto
        };
    }

    public static List<RepairTaskDto> ToDtos(this IEnumerable<RepairTask> entities, IReadOnlyDictionary<Guid, InventoryItem> inventoryItemsMap)
    {
        return [.. entities.Select(e => e.ToDto(inventoryItemsMap))];
    }

    // Mapping from WorkOrderTask (Snapshot with fixed prices)
    public static RepairTaskDto ToDto(this WorkOrderTask entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new RepairTaskDto
        {
            Id = entity.Id,
            OriginalRepairTaskId = entity.OriginalRepairTaskId,
            Name = entity.Name,
            LaborCost = entity.LaborCost,
            TotalCost = entity.TotalCost,
            EstimatedDurationInMins = entity.EstimatedDurationInMins,
            Parts = entity.Parts.Select(p => p.ToDto()).ToList()
        };
    }

    public static List<RepairTaskDto> ToDtos(this IEnumerable<WorkOrderTask> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }

    // Mapping WorkOrderTaskPart Snapshot to RepairTaskPartDto
    public static RepairTaskPartDto ToDto(this WorkOrderTaskPart entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new RepairTaskPartDto
        {
            InventoryItemId = entity.InventoryItemId,
            Name = entity.Name,
            Cost = entity.Cost,
            Quantity = entity.Quantity
        };
    }

    public static List<RepairTaskPartDto> ToDtos(this IEnumerable<WorkOrderTaskPart> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }
}