using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Reports.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Reports.Queries.GetPartsUsageReport;

public sealed class GetPartsUsageReportQueryHandler(IAppDbContext context)
    : IRequestHandler<GetPartsUsageReportQuery, Result<PartsUsageReportDto>>
{
    public async Task<Result<PartsUsageReportDto>> Handle(GetPartsUsageReportQuery query, CancellationToken ct)
    {
        // 1. Convert the required time range from local time to UTC
        var localStart = query.FromDate.ToDateTime(TimeOnly.MinValue);
        var localEnd = query.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, query.TimeZone);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd, query.TimeZone);

        // 2. Fetch completed work orders within this range along with tasks and parts
        var completedOrders = await context.WorkOrders
            .Where(w =>
                w.State == WorkOrderState.Completed &&
                w.StartAtUtc >= utcStart &&
                w.StartAtUtc < utcEnd)
            .Include(w => w.RepairTasks)
                .ThenInclude(t => t.Parts)
            .AsNoTracking()
            .ToListAsync(ct);

        if (completedOrders.Count == 0)
        {
            return new PartsUsageReportDto
            {
                FromDate = query.FromDate,
                ToDate = query.ToDate,
                TotalPartsQuantityUsed = 0,
                TotalPartsCost = 0m,
                DistinctPartsCount = 0,
                Parts = []
            };
        }

        // 3. Flatten used parts
        var usedPartsList = completedOrders
            .SelectMany(w => w.RepairTasks
                .SelectMany(t => t.Parts)
                .Select(p => new { WorkOrderId = w.Id, Part = p }))
            .ToList();

        var usedItemIds = usedPartsList
            .Select(x => x.Part.InventoryItemId)
            .Distinct()
            .ToList();

        // 4. Fetch current inventory balances with IgnoreQueryFilters
        var inventoryItems = await context.InventoryItems
            .IgnoreQueryFilters()
            .Where(i => usedItemIds.Contains(i.Id))
            .AsNoTracking()
            .ToDictionaryAsync(i => i.Id, ct);

        // 5. Aggregate statistics per part
        var groupedParts = usedPartsList
            .GroupBy(x => x.Part.InventoryItemId)
            .ToList();

        var partUsageItems = new List<PartUsageItemDto>(groupedParts.Count);

        foreach (var group in groupedParts)
        {
            var itemId = group.Key;
            var partsInGroup = group.Select(x => x.Part).ToList();
            var distinctOrdersCount = group.Select(x => x.WorkOrderId).Distinct().Count();

            inventoryItems.TryGetValue(itemId, out var invItem);

            var totalQty = partsInGroup.Sum(p => p.Quantity);
            var totalCost = partsInGroup.Sum(p => p.Cost * p.Quantity);

            var averageUnitCost = totalQty > 0
                ? Math.Round(totalCost / totalQty, 2)
                : (invItem?.Cost ?? partsInGroup.First().Cost);

            var partName = invItem?.Name ?? partsInGroup.First().Name;
            var currentStock = invItem?.StockQuantity ?? 0;
            var reorderLevel = invItem?.ReorderLevel ?? 0;

            partUsageItems.Add(new PartUsageItemDto
            {
                InventoryItemId = itemId,
                PartName = partName,
                QuantityUsed = totalQty,
                AverageUnitCost = averageUnitCost,
                TotalCost = totalCost,
                WorkOrdersCount = distinctOrdersCount,
                CurrentStock = currentStock,
                ReorderLevel = reorderLevel,
                IsLowStock = currentStock <= reorderLevel
            });
        }

        // 6. Overall statistics
        var totalQuantity = partUsageItems.Sum(p => p.QuantityUsed);
        var totalCostSum = partUsageItems.Sum(p => p.TotalCost);

        return new PartsUsageReportDto
        {
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            TotalPartsQuantityUsed = totalQuantity,
            TotalPartsCost = totalCostSum,
            DistinctPartsCount = partUsageItems.Count,
            Parts = partUsageItems.OrderByDescending(p => p.QuantityUsed).ToList()
        };
    }
}