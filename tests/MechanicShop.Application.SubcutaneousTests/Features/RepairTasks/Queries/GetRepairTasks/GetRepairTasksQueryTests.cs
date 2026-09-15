using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.RepairTasks.Queries.GetRepairTasks;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Tests.Common.RepairTasks;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.RepairTasks.Queries.GetRepairTasks;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetRepairTasksQueryTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WhenRepairTasksExist_ShouldReturnListWithAllRepairTasksAndTheirParts()
    {
        var task1 = RepairTaskFactory.CreateRepairTask().Value;
        var task2 = RepairTaskFactory.CreateRepairTask().Value;

        using (var arrangeScope = _factory.CreateScope())
        {
            var context = arrangeScope.ServiceProvider.GetRequiredService<IAppDbContext>();
            await context.RepairTasks.AddRangeAsync(task1, task2);
            await context.SaveChangesAsync(default);
        }

        var query = new GetRepairTasksQuery();

        using var actScope = _factory.CreateScope();
        var mediator = actScope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        result.Value.Should().HaveCount(2);
        result.Value.Any(t => t.OriginalRepairTaskId == task1.Id).Should().BeTrue();
        result.Value.Any(t => t.OriginalRepairTaskId == task2.Id).Should().BeTrue();

        result.Value.ForEach(t => t.Parts.Should().NotBeNull());
    }

    [Fact]
    public async Task Handle_WhenNoRepairTasksExist_ShouldReturnEmptyList()
    {
        var query = new GetRepairTasksQuery();

        using var actScope = _factory.CreateScope();
        var mediator = actScope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }
}