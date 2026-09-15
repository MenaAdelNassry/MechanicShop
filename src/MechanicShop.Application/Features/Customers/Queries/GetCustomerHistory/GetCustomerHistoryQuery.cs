using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.Customers.Dtos.History;
using MechanicShop.Domain.Common.Results;

namespace MechanicShop.Application.Features.Customers.Queries.GetCustomerHistory;

public sealed record GetCustomerHistoryQuery(Guid CustomerId) : ICachedQuery<Result<CustomerHistoryDto>>
{
    public string CacheKey => $"customers:history:{CustomerId}";

    public TimeSpan Expiration => TimeSpan.FromMinutes(10);

    public string[] Tags => [$"customer_{CustomerId}"];
}