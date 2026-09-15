using MechanicShop.Domain.Workorders.Enums;

namespace MechanicShop.Api.Requests.WorkOrders;

public class RelocateWorkOrderRequest
{
    public DateTimeOffset NewStartAtUtc { get; set; }
    public Spot NewSpot { get; set; }
}