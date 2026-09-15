using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Common.Models;
using MechanicShop.Application.Features.Customers.Dtos;
using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Application.Features.Customers.Queries.GetCustomers;

public sealed record GetCustomersQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null
) : ICachedQuery<Result<PaginatedList<CustomerDto>>>
{
    public string CacheKey => $"customers:p:{PageNumber}:s:{PageSize}:q:{SearchTerm?.Trim().ToLower()}";

    public TimeSpan Expiration => TimeSpan.FromMinutes(5);

    public string[] Tags => ["customer_list"];
}