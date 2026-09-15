using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MechanicShop.Infrastructure.RealTime;

public sealed class WorkOrderHub : Hub
{
    public const string HubUrl = "/hubs/workorders";
    public const string StaffGroupName = "StaffGroup";

    [Authorize]
    public async Task JoinStaffGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, StaffGroupName);
    }

    [Authorize]
    public async Task LeaveStaffGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, StaffGroupName);
    }

    public async Task JoinTrackingGroup(string trackingToken)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tracking_{trackingToken}");
    }

    public async Task LeaveTrackingGroup(string trackingToken)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tracking_{trackingToken}");
    }
}