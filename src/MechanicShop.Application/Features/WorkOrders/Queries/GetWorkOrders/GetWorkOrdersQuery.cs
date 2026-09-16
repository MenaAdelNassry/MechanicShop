using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Common.Models;
using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Domain.Common.Results;
using MechanicShop.Domain.Workorders.Enums;

namespace MechanicShop.Application.Features.WorkOrders.Queries.GetWorkOrders;

public sealed record GetWorkOrdersQuery(
    int Page = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    string SortColumn = "createdAt",
    string SortDirection = "desc",
    WorkOrderState? State = null,
    Guid? VehicleId = null,
    Guid? LaborId = null,
    DateTime? StartDateFrom = null,
    DateTime? StartDateTo = null,
    DateTime? EndDateFrom = null,
    DateTime? EndDateTo = null,
    Guid? SpotId = null
) : ICachedQuery<Result<PaginatedList<WorkOrderListItemDto>>>
{
    public string CacheKey =>
        $"workorders:p:{Page}:s:{PageSize}" +
        $":q:{SearchTerm?.Trim().ToLower() ?? "-"}" +
        $":sort:{SortColumn}:{SortDirection}" +
        $":st:{State?.ToString() ?? "-"}" +
        $":v:{VehicleId?.ToString() ?? "-"}" +
        $":l:{LaborId?.ToString() ?? "-"}" +
        $":sdf:{StartDateFrom?.ToString("yyyyMMdd") ?? "-"}" +
        $":sdt:{StartDateTo?.ToString("yyyyMMdd") ?? "-"}" +
        $":edf:{EndDateFrom?.ToString("yyyyMMdd") ?? "-"}" +
        $":edt:{EndDateTo?.ToString("yyyyMMdd") ?? "-"}" +
        $":sp:{SpotId?.ToString() ?? "-"}";

    public TimeSpan Expiration => TimeSpan.FromMinutes(5);

    public string[] Tags => ["workorder_list"];
}