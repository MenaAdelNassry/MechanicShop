using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Common.Models;
using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Application.Features.Billing.Mappers;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Billing.Queries.GetInvoices;

public sealed class GetInvoicesQueryHandler(IAppDbContext context)
    : IRequestHandler<GetInvoicesQuery, Result<PaginatedList<InvoiceDto>>>
{
    public async Task<Result<PaginatedList<InvoiceDto>>> Handle(GetInvoicesQuery request, CancellationToken ct)
    {
        var pageNumber = request.PageNumber;
        var pageSize = request.PageSize;

        var query = context.Invoices
            .Include(i => i.Payments)
            .Include(i => i.LineItems)
            .Include(i => i.WorkOrder)
                .ThenInclude(w => w!.Vehicle)
                    .ThenInclude(v => v!.Customer)
            .AsNoTracking();

        if (request.FromDateUtc.HasValue)
        {
            query = query.Where(i => i.IssuedAtUtc >= request.FromDateUtc.Value);
        }

        if (request.ToDateUtc.HasValue)
        {
            query = query.Where(i => i.IssuedAtUtc <= request.ToDateUtc.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(i => i.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();

            query = query.Where(i =>
                (i.WorkOrder != null && i.WorkOrder.Vehicle != null && i.WorkOrder.Vehicle.LicensePlate.Contains(term)) ||
                (i.WorkOrder != null && i.WorkOrder.Vehicle != null && i.WorkOrder.Vehicle.Customer != null &&
                    (string.Concat(i.WorkOrder.Vehicle.Customer.Name.FirstName, " ", i.WorkOrder.Vehicle.Customer.Name.LastName).Contains(term) ||
                     i.WorkOrder.Vehicle.Customer.Name.FirstName.Contains(term) ||
                     i.WorkOrder.Vehicle.Customer.Name.LastName.Contains(term) ||
                     ((string)(object)i.WorkOrder.Vehicle.Customer.PhoneNumber).Contains(term))));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(i => i.IssuedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = items.ToDtos();

        return new PaginatedList<InvoiceDto>()
        {
            Items = dtos,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}