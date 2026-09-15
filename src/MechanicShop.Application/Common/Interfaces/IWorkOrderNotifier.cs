using MechanicShop.Domain.Workorders.Enums;

namespace MechanicShop.Application.Common.Interfaces;

public interface IWorkOrderNotifier
{
    Task NotifyWorkOrdersChangedAsync(CancellationToken ct = default);
    Task NotifyCustomerTrackingUpdatedAsync(
        Guid trackingToken,
        WorkOrderState newState,
        DateTimeOffset? timestampUtc,
        CancellationToken ct = default);
}