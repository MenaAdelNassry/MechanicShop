using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Customers.Dtos.History;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Enums;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Customers.Queries.GetCustomerHistory;

public sealed class GetCustomerHistoryQueryHandler(IAppDbContext context)
    : IRequestHandler<GetCustomerHistoryQuery, Result<CustomerHistoryDto>>
{
    public async Task<Result<CustomerHistoryDto>> Handle(GetCustomerHistoryQuery request, CancellationToken ct)
    {
        // 1. Get Customer with Vehicles
        var customer = await context.Customers
            .Include(c => c.Vehicles)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, ct);

        if (customer is null)
        {
            return ApplicationErrors.Customers.NotFound;
        }

        var vehicleIds = customer.Vehicles.Select(v => v.Id).ToList();

        // 2. Get previous work orders for this customer's vehicles
        var workOrders = await context.WorkOrders
            .Where(w => vehicleIds.Contains(w.VehicleId))
            .Include(w => w.Labor)
            .Include(w => w.Vehicle)
            .Include(w => w.RepairTasks)
                .ThenInclude(t => t.Parts)
            .OrderByDescending(w => w.StartAtUtc)
            .AsNoTracking()
            .ToListAsync(ct);

        // 3. Calculate completed orders and total lifetime spent
        var completedOrders = workOrders.Where(w => w.State == WorkOrderState.Completed).ToList();
        var totalLifetimeSpent = completedOrders.Sum(w => w.Total);

        // 4. Aggregate each vehicle's history with the last completed service date
        var vehicleDtos = customer.Vehicles.Select(v =>
        {
            var lastServiceDate = workOrders
                .Where(w => w.VehicleId == v.Id && w.State == WorkOrderState.Completed)
                .OrderByDescending(w => w.StartAtUtc)
                .Select(w => (DateTimeOffset?)w.StartAtUtc)
                .FirstOrDefault();

            return new CustomerVehicleHistoryDto(
                v.Id,
                v.Make,
                v.Model,
                v.Year,
                v.LicensePlate,
                lastServiceDate);
        }).ToList();

        // 5. Build the timeline for work orders
        var timelineDtos = workOrders.Select(w => new CustomerWorkOrderTimelineDto(
            w.Id,
            w.VehicleId,
            w.Vehicle != null ? w.Vehicle.VehicleInfo : "Unknown Vehicle",
            w.StartAtUtc,
            w.EndAtUtc,
            w.State,
            w.Spot,
            w.Labor?.Name.FullName,
            w.TotalLaborCost,
            w.TotalPartsCost,
            w.Total,
            w.RepairTasks.Select(t => t.Name).ToList())).ToList();

        return new CustomerHistoryDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name.FullName,
            Email = customer.Email.Value,
            PhoneNumber = customer.PhoneNumber.Value,
            TotalVisits = completedOrders.Count,
            TotalLifetimeSpent = totalLifetimeSpent,
            Vehicles = vehicleDtos,
            WorkOrders = timelineDtos
        };
    }
}