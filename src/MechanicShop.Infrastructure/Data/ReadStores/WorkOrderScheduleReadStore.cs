using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Workorders.Enums;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Infrastructure.Data.ReadStores;

public sealed class WorkOrderScheduleReadStore(IAppDbContext context) : IWorkOrderScheduleReadStore
{
    public async Task<bool> HasSpotConflictAsync(Spot spot, DateTimeOffset startAt, DateTimeOffset endAt, Guid? excludeWorkOrderId = default, CancellationToken ct = default)
    {
        return await context.WorkOrders
            .AsNoTracking()
            .AnyAsync(
                wo =>
                wo.Spot == spot &&
                wo.State != WorkOrderState.Cancelled &&
                wo.StartAtUtc < endAt &&
                wo.EndAtUtc > startAt &&
                (excludeWorkOrderId == null || wo.Id != excludeWorkOrderId),
                ct);
    }

    public async Task<bool> HasVehicleConflictAsync(Guid vehicleId, DateTimeOffset startAt, DateTimeOffset endAt, Guid? excludeWorkOrderId = default, CancellationToken ct = default)
    {
        return await context.WorkOrders
            .AsNoTracking()
            .AnyAsync(
                wo =>
                wo.VehicleId == vehicleId &&
                wo.State != WorkOrderState.Cancelled &&
                wo.StartAtUtc < endAt &&
                wo.EndAtUtc > startAt &&
                (excludeWorkOrderId == null || wo.Id != excludeWorkOrderId),
                ct);
    }

    public async Task<bool> HasLaborConflictAsync(Guid laborId, DateTimeOffset startAt, DateTimeOffset endAt, Guid? excludeWorkOrderId = default, CancellationToken ct = default)
    {
        return await context.WorkOrders
            .AsNoTracking()
            .AnyAsync(
                wo =>
                wo.LaborId == laborId &&
                wo.State != WorkOrderState.Cancelled &&
                wo.StartAtUtc < endAt &&
                wo.EndAtUtc > startAt &&
                (excludeWorkOrderId == null || wo.Id != excludeWorkOrderId),
                ct);
    }
}