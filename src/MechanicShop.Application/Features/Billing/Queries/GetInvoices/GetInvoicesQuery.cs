using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Common.Models;
using MechanicShop.Application.Features.Billing.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Billing;

namespace MechanicShop.Application.Features.Billing.Queries.GetInvoices;

public sealed record GetInvoicesQuery(
    int PageNumber = 1,
    int PageSize = 10,
    DateTimeOffset? FromDateUtc = null,
    DateTimeOffset? ToDateUtc = null,
    InvoiceStatus? Status = null,
    string? SearchTerm = null
) : ICachedQuery<Result<PaginatedList<InvoiceDto>>>
{
    public string CacheKey =>
        $"invoices:p:{PageNumber}:s:{PageSize}:from:{FromDateUtc:yyyyMMdd}:to:{ToDateUtc:yyyyMMdd}:st:{Status}:q:{SearchTerm}";

    public TimeSpan Expiration => TimeSpan.FromMinutes(5);

    public string[] Tags => ["invoice_list"];
}