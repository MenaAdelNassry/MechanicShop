using MassTransit;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.Consumers;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Domain.Workorders.Events;

using NSubstitute;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.WorkOrders.EventHandlers;

[Collection(WebAppFactoryCollection.CollectionName)]
public class WorkOrderCollectionModifiedEventHandlerTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    [Fact]
    public async Task Handle_WhenEventIsTriggered_ShouldCallNotifyWorkOrdersChangedAsync()
    {
        // Arrange
        var mockNotifier = Substitute.For<IWorkOrderNotifier>();

        var handler = new WorkOrderCollectionModifiedEventConsumer(mockNotifier);
        var notification = new WorkOrderCollectionModified();

        // Act
        var consumeContext = Substitute.For<ConsumeContext<WorkOrderCollectionModified>>();
        consumeContext.Message.Returns(notification);
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await handler.Consume(consumeContext);

        // Assert
        await mockNotifier
            .Received(1)
            .NotifyWorkOrdersChangedAsync(Arg.Any<CancellationToken>());
    }
}