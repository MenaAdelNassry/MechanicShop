using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Domain.Workorders.Enums;

using Microsoft.AspNetCore.SignalR;

namespace MechanicShop.Infrastructure.RealTime;

public sealed class SignalRWorkOrderNotifier(IHubContext<WorkOrderHub> hubContext) : IWorkOrderNotifier
{
    private readonly IHubContext<WorkOrderHub> _hubContext = hubContext;

    public Task NotifyCustomerTrackingUpdatedAsync(
        Guid trackingToken,
        WorkOrderState newState,
        DateTimeOffset? timestampUtc,
        CancellationToken ct = default)
    {
        var groupName = $"tracking_{trackingToken}";

        var payload = new
        {
            TrackingToken = trackingToken,
            NewState = newState,
            TimestampUtc = timestampUtc ?? DateTimeOffset.UtcNow
        };

        return _hubContext.Clients.Group(groupName)
            .SendAsync("TrackingStateChanged", payload, cancellationToken: ct);
    }

    public Task NotifyWorkOrdersChangedAsync(CancellationToken ct = default) =>
    _hubContext.Clients.Group(WorkOrderHub.StaffGroupName)
        .SendAsync("WorkOrdersChanged", cancellationToken: ct);
}