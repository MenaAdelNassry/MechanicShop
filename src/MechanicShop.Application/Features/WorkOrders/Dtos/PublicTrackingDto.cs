using MechanicShop.Domain.Workorders.Billing;
using MechanicShop.Domain.Workorders.Enums;

namespace MechanicShop.Application.Features.WorkOrders.Dtos;

public sealed record PublicTrackingDto(
    Guid TrackingToken,
    string VehicleMake,
    string VehicleModel,
    string LicensePlate,
    WorkOrderState CurrentState,
    DateTimeOffset ScheduledStartUtc,
    DateTimeOffset ScheduledEndUtc,
    IReadOnlyList<string> ServiceNames,
    bool HasInvoice,
    InvoiceStatus? InvoiceStatus,
    decimal? RemainingBalance,
    Guid? InvoiceId
);