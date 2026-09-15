namespace MechanicShop.Application.Features.Customers.Dtos.History;

public sealed record CustomerHistoryDto
{
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public int TotalVisits { get; init; }
    public decimal TotalLifetimeSpent { get; init; }
    public IReadOnlyList<CustomerVehicleHistoryDto> Vehicles { get; init; } = [];
    public IReadOnlyList<CustomerWorkOrderTimelineDto> WorkOrders { get; init; } = [];
}