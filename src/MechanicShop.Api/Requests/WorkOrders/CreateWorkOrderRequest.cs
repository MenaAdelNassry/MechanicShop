namespace MechanicShop.Api.Requests.WorkOrders;

public class CreateWorkOrderRequest
{
    public Guid SpotId { get; set; }

    public Guid VehicleId { get; set; }

    public Guid LaborId { get; set; }

    public List<Guid> RepairTaskIds { get; set; } = [];

    public DateTimeOffset StartAtUtc { get; set; }
}