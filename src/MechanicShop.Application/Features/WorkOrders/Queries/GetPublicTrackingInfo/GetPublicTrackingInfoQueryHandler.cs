using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Billing;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.WorkOrders.Queries.GetPublicTrackingInfo;

public sealed class GetPublicTrackingInfoQueryHandler(IAppDbContext context)
    : IRequestHandler<GetPublicTrackingInfoQuery, Result<PublicTrackingDto>>
{
    public async Task<Result<PublicTrackingDto>> Handle(GetPublicTrackingInfoQuery request, CancellationToken ct)
    {
        var workOrder = await context.WorkOrders
            .AsNoTracking()
            .Include(w => w.Vehicle)
            .Include(w => w.RepairTasks)
            .Include(w => w.Invoice)
                .ThenInclude(i => i!.Payments)
            .FirstOrDefaultAsync(w => w.TrackingToken == request.TrackingToken, ct);

        if (workOrder is null)
        {
            return ApplicationErrors.WorkOrders.NotFound;
        }

        var vehicleMake = workOrder.Vehicle?.Make ?? string.Empty;
        var vehicleModel = workOrder.Vehicle?.Model ?? string.Empty;
        var licensePlate = workOrder.Vehicle?.LicensePlate ?? string.Empty;

        var serviceNames = workOrder.RepairTasks
            .Select(t => t.Name)
            .ToList();

        var hasInvoice = workOrder.Invoice is not null;
        InvoiceStatus? invoiceStatus = workOrder.Invoice?.Status;
        decimal? remainingBalance = workOrder.Invoice?.RemainingAmount;
        Guid? invoiceId = workOrder.Invoice?.Id;

        var dto = new PublicTrackingDto(
            TrackingToken: workOrder.TrackingToken,
            VehicleMake: vehicleMake,
            VehicleModel: vehicleModel,
            LicensePlate: licensePlate,
            CurrentState: workOrder.State,
            ScheduledStartUtc: workOrder.StartAtUtc,
            ScheduledEndUtc: workOrder.EndAtUtc,
            ServiceNames: serviceNames,
            HasInvoice: hasInvoice,
            InvoiceStatus: invoiceStatus,
            RemainingBalance: remainingBalance,
            InvoiceId: invoiceId);

        return dto;
    }
}