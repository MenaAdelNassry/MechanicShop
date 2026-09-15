using MassTransit;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.EventHandlers;
using MechanicShop.Domain.Workorders.Events;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Application.Features.WorkOrders.Consumers;

public sealed class SendWorkOrderCompletedSmsConsumer(
    ISmsSender smsSender,
    IAppDbContext context,
    ILogger<SendWorkOrderCompletedSmsConsumer> logger)
    : IConsumer<WorkOrderCompletedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<WorkOrderCompletedIntegrationEvent> consumeContext)
    {
        var message = consumeContext.Message;
        var ct = consumeContext.CancellationToken;

        var workOrder = await context.WorkOrders
            .Include(w => w.Vehicle!)
                .ThenInclude(v => v.Customer)
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == message.WorkOrderId, ct);

        if (workOrder is null)
        {
            logger.LogError("SMS Dispatch Cancelled: WorkOrder '{WorkOrderId}' does not exist.", message.WorkOrderId);
            return;
        }

        var customerPhone = workOrder.Vehicle?.Customer?.PhoneNumber?.Value;

        if (string.IsNullOrWhiteSpace(customerPhone))
        {
            logger.LogWarning("SMS Dispatch Skipped: Customer phone number is missing for WorkOrder '{WorkOrderId}'.", message.WorkOrderId);
            return;
        }

        var vehiclePlate = workOrder.Vehicle?.LicensePlate ?? "your vehicle";
        var smsText = $"MechanicShop: Service for {vehiclePlate} is completed and ready for pickup. Thank you!";

        await smsSender.SendSmsAsync(customerPhone, smsText, ct);
    }
}