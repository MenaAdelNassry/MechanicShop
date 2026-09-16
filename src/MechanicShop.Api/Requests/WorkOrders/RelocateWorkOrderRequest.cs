using MechanicShop.Domain.Workorders.Enums;

namespace MechanicShop.Api.Requests.WorkOrders;

public class RelocateWorkOrderRequest
{
    public DateTimeOffset NewStartAtUtc { get; set; }
    public Guid NewSpotId { get; set; }
}