using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Common.Models;
using MechanicShop.Application.Features.Customers.Dtos;
using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.WorkOrders.Queries.GetWorkOrders;

public sealed class GetWorkOrdersQueryHandler(IAppDbContext context)
    : IRequestHandler<GetWorkOrdersQuery, Result<PaginatedList<WorkOrderListItemDto>>>
{
    public async Task<Result<PaginatedList<WorkOrderListItemDto>>> Handle(GetWorkOrdersQuery query, CancellationToken ct)
    {
        var workOrdersQuery = context.WorkOrders.AsNoTracking();

        workOrdersQuery = ApplyFilters(workOrdersQuery, query);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            workOrdersQuery = ApplySearchTerm(workOrdersQuery, query.SearchTerm);
        }

        workOrdersQuery = ApplySorting(workOrdersQuery, query.SortColumn, query.SortDirection);

        var count = await workOrdersQuery.CountAsync(cancellationToken: ct);

        var items = await workOrdersQuery
              .Skip((query.Page - 1) * query.PageSize)
              .Take(query.PageSize)
              .Select(wo => new WorkOrderListItemDto
              {
                  WorkOrderId = wo.Id,
                  InvoiceId = wo.Invoice == null ? null : wo.Invoice.Id,
                  Spot = wo.Spot,
                  StartAtUtc = wo.StartAtUtc,
                  EndAtUtc = wo.EndAtUtc,
                  Vehicle = wo.Vehicle == null ? null : new VehicleDto(
                    wo.Vehicle.Id,
                    wo.Vehicle.Make,
                    wo.Vehicle.Model,
                    wo.Vehicle.Year,
                    wo.Vehicle.LicensePlate),
                  Customer = (wo.Vehicle != null && wo.Vehicle.Customer != null)
                    ? wo.Vehicle.Customer.Name.FirstName + " " + wo.Vehicle.Customer.Name.LastName
                    : "No Customer",
                  Labor = wo.Labor != null
                    ? wo.Labor.Name.FirstName + " " + wo.Labor.Name.LastName
                    : null,
                  State = wo.State,
                  RepairTasks = wo.RepairTasks.Select(rt => rt.Name).ToList()
              })
            .ToListAsync(ct);

        return new PaginatedList<WorkOrderListItemDto>
        {
            Items = items,
            PageNumber = query.Page,
            PageSize = query.PageSize,
            TotalCount = count,
            TotalPages = (int)Math.Ceiling(count / (double)query.PageSize)
        };
    }

    private static IQueryable<WorkOrder> ApplyFilters(IQueryable<WorkOrder> query, GetWorkOrdersQuery searchQuery)
    {
        if (searchQuery.State.HasValue)
        {
            query = query.Where(wo => wo.State == searchQuery.State.Value);
        }

        if (searchQuery.VehicleId.HasValue && searchQuery.VehicleId != Guid.Empty)
        {
            query = query.Where(wo => wo.VehicleId == searchQuery.VehicleId.Value);
        }

        if (searchQuery.LaborId.HasValue && searchQuery.LaborId != Guid.Empty)
        {
            query = query.Where(wo => wo.LaborId == searchQuery.LaborId.Value);
        }

        if (searchQuery.StartDateFrom.HasValue)
        {
            query = query.Where(wo => wo.StartAtUtc >= searchQuery.StartDateFrom.Value);
        }

        if (searchQuery.StartDateTo.HasValue)
        {
            query = query.Where(wo => wo.StartAtUtc <= searchQuery.StartDateTo.Value);
        }

        if (searchQuery.EndDateFrom.HasValue)
        {
            query = query.Where(wo => wo.EndAtUtc >= searchQuery.EndDateFrom.Value);
        }

        if (searchQuery.EndDateTo.HasValue)
        {
            query = query.Where(wo => wo.EndAtUtc <= searchQuery.EndDateTo.Value);
        }

        if (searchQuery.Spot.HasValue)
        {
            query = query.Where(wo => wo.Spot == searchQuery.Spot.Value);
        }

        return query;
    }

    private static IQueryable<WorkOrder> ApplySearchTerm(IQueryable<WorkOrder> query, string searchTerm)
    {
        var normalized = searchTerm.Trim().ToLower();

        return query.Where(wo =>
        (wo.Vehicle != null && (
            wo.Vehicle.Make.Contains(normalized) ||
            wo.Vehicle.Model.Contains(normalized) ||
            wo.Vehicle.LicensePlate.Contains(normalized)
        )) ||
        (wo.Labor != null && (
            wo.Labor.Name.FirstName.Contains(normalized) ||
            wo.Labor.Name.LastName.Contains(normalized)
        )) ||
        wo.RepairTasks.Any(rt => rt.Name.Contains(normalized)) ||
        wo.Id.ToString().Contains(normalized));
    }

    private static IQueryable<WorkOrder> ApplySorting(IQueryable<WorkOrder> query, string sortColumn, string sortDirection)
    {
        var isDescending = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase) ||
                           sortDirection.Equals("descending", StringComparison.OrdinalIgnoreCase);

        var column = sortColumn.Trim().ToLowerInvariant();

        return column switch
        {
            "createdat" or "createdatutc" => isDescending ? query.OrderByDescending(wo => wo.CreatedAtUtc) : query.OrderBy(wo => wo.CreatedAtUtc),
            "updatedat" or "updatedatutc" => isDescending ? query.OrderByDescending(wo => wo.LastModifiedUtc) : query.OrderBy(wo => wo.LastModifiedUtc),
            "startat" or "startatutc" => isDescending ? query.OrderByDescending(wo => wo.StartAtUtc) : query.OrderBy(wo => wo.StartAtUtc),
            "endat" or "endatutc" => isDescending ? query.OrderByDescending(wo => wo.EndAtUtc) : query.OrderBy(wo => wo.EndAtUtc),
            "state" => isDescending ? query.OrderByDescending(wo => wo.State) : query.OrderBy(wo => wo.State),
            "spot" => isDescending ? query.OrderByDescending(wo => wo.Spot) : query.OrderBy(wo => wo.Spot),
            "total" => isDescending
                ? query.OrderByDescending(wo => wo.RepairTasks.Sum(rt => rt.LaborCost) + wo.RepairTasks.SelectMany(rt => rt.Parts).Sum(p => p.Cost * p.Quantity))
                : query.OrderBy(wo => wo.RepairTasks.Sum(rt => rt.LaborCost) + wo.RepairTasks.SelectMany(rt => rt.Parts).Sum(p => p.Cost * p.Quantity)),
            "vehicleid" => isDescending ? query.OrderByDescending(wo => wo.VehicleId) : query.OrderBy(wo => wo.VehicleId),
            "laborid" => isDescending ? query.OrderByDescending(wo => wo.LaborId) : query.OrderBy(wo => wo.LaborId),
            _ => isDescending ? query.OrderByDescending(wo => wo.CreatedAtUtc) : query.OrderBy(wo => wo.CreatedAtUtc)
        };
    }
}