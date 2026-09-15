using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Common.Models;
using MechanicShop.Application.Features.Customers.Dtos;
using MechanicShop.Application.Features.Customers.Mappers;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace MechanicShop.Application.Features.Customers.Queries.GetCustomers;

public sealed class GetCustomersQueryHandler(IAppDbContext context)
    : IRequestHandler<GetCustomersQuery, Result<PaginatedList<CustomerDto>>>
{
    public async Task<Result<PaginatedList<CustomerDto>>> Handle(GetCustomersQuery request, CancellationToken ct)
    {
        var query = context.Customers
            .Include(c => c.Vehicles)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(c =>
                c.Name.FirstName.Contains(term) ||
                c.Name.LastName.Contains(term) ||
                ((string)(object)c.PhoneNumber).Contains(term) ||
                ((string)(object)c.Email).Contains(term) ||
                c.Vehicles.Any(v => v.LicensePlate.Contains(term)));
        }

        var totalCount = await query.CountAsync(ct);

        var customers = await query
            .OrderBy(c => c.Name.LastName)
            .ThenBy(c => c.Name.FirstName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        var dtos = customers.ToDtos();

        return new PaginatedList<CustomerDto>()
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}