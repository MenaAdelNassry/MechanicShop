using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Enums;

namespace MechanicShop.Application.Common.Interfaces;

public interface IWorkOrderScheduleReadStore
{
    Task<bool> HasSpotConflictAsync(Spot spot, DateTimeOffset startAt, DateTimeOffset endAt, Guid? excludeWorkOrderId = default, CancellationToken ct = default);
    Task<bool> HasVehicleConflictAsync(Guid vehicleId, DateTimeOffset startAt, DateTimeOffset endAt, Guid? excludeWorkOrderId = default, CancellationToken ct = default);
    Task<bool> HasLaborConflictAsync(Guid laborId, DateTimeOffset startAt, DateTimeOffset endAt, Guid? excludeWorkOrderId = default, CancellationToken ct = default);
}