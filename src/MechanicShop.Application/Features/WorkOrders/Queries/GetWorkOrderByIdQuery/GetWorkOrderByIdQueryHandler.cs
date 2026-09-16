using MechanicShop.Application.Common.Errors;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Application.Features.WorkOrders.Mappers;
using MechanicShop.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Queries.GetWorkOrderByIdQuery;

public sealed class GetWorkOrderByIdQueryHandler(
    ILogger<GetWorkOrderByIdQueryHandler> logger,
    IAppDbContext context)
    : IRequestHandler<GetWorkOrderByIdQuery, Result<WorkOrderDto>>
{
    public async Task<Result<WorkOrderDto>> Handle(GetWorkOrderByIdQuery query, CancellationToken ct)
    {
        var workOrder = await context.WorkOrders
            .AsNoTracking()
            .Include(a => a.RepairTasks)
                .ThenInclude(a => a.Parts)
            .Include(a => a.Labor)
            .Include(a => a.Vehicle!)
                .ThenInclude(v => v.Customer)
            .Include(a => a.Invoice)
            .Include(a => a.Spot)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == query.WorkOrderId, ct);

        if (workOrder is null)
        {
            logger.LogWarning("WorkOrder with id {WorkOrderId} was not found", query.WorkOrderId);
            return ApplicationErrors.WorkOrders.NotFound;
        }

        return workOrder.ToDto();
    }
}