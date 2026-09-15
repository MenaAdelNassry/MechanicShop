using MechanicShop.Application.Features.Customers.Mappers;
using MechanicShop.Application.Features.Employees.Mappers;
using MechanicShop.Application.Features.RepairTasks.Mappers;
using MechanicShop.Application.Features.WorkOrders.Dtos;
using MechanicShop.Domain.Customers.Vehicles;
using MechanicShop.Domain.Employees;
using MechanicShop.Domain.Workorders;

using static MechanicShop.Application.Common.Errors.ApplicationErrors;

namespace MechanicShop.Application.Features.WorkOrders.Mappers;

public static class WorkOrderMapper
{
    public static WorkOrderDto ToDto(this WorkOrder workOrder, Vehicle? vehicle = null, Employee? labor = null)
    {
        ArgumentNullException.ThrowIfNull(workOrder);

        var vehicleEntity = vehicle ?? workOrder.Vehicle;
        var laborEntity = labor ?? workOrder.Labor;

        return new WorkOrderDto
        {
            WorkOrderId = workOrder.Id,
            Spot = workOrder.Spot,
            StartAtUtc = workOrder.StartAtUtc,
            EndAtUtc = workOrder.EndAtUtc,
            Labor = laborEntity?.ToDto(),
            RepairTasks = workOrder.RepairTasks.ToDtos(),
            Vehicle = vehicleEntity is null ? null : vehicleEntity.ToDto(),
            State = workOrder.State,
            TotalPartCost = workOrder.TotalPartsCost,
            TotalLaborCost = workOrder.TotalLaborCost,
            TotalCost = workOrder.Total,
            TotalDurationInMins = workOrder.RepairTasks.Sum(rt => (int)rt.EstimatedDurationInMins),
            InvoiceId = workOrder.Invoice?.Id,
            CreatedAt = workOrder.CreatedAtUtc,
            ActualStartedAtUtc = workOrder.ActualStartedAtUtc,
            ActualCompletedAtUtc = workOrder.ActualCompletedAtUtc,
        };
    }

    public static List<WorkOrderDto> ToDtos(this IEnumerable<WorkOrder> entities)
    {
        return [.. entities.Select(e => e.ToDto())];
    }

    public static WorkOrderListItemDto ToListItemDto(this WorkOrder entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new WorkOrderListItemDto
        {
            WorkOrderId = entity.Id,
            Spot = entity.Spot,
            StartAtUtc = entity.StartAtUtc,
            EndAtUtc = entity.EndAtUtc,
            Vehicle = entity.Vehicle!.ToDto(),
            Labor = entity.Labor is null ? null : $"{entity.Labor.Name.FullName}",
            State = entity.State,
            RepairTasks = entity.RepairTasks.Select(rt => rt.Name).ToList()
        };
    }
}