using MechanicShop.Domain.Workorders.Enums;

namespace MechanicShop.Application.Features.Customers.Dtos.History;
public sealed record CustomerWorkOrderTimelineDto(
    Guid WorkOrderId,
    Guid VehicleId,
    string VehicleInfo,
    DateTimeOffset StartAtUtc,
    DateTimeOffset EndAtUtc,
    WorkOrderState State,
    Guid SpotId,
    string? LaborName,
    decimal TotalLaborCost,
    decimal TotalPartsCost,
    decimal TotalCost,
    IReadOnlyList<string> TasksSummary
);