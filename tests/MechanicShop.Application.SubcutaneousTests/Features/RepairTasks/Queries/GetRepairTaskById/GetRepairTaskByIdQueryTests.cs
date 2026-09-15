using FluentAssertions;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.RepairTasks.Queries.GetRepairTaskById;
using MechanicShop.Application.SubcutaneousTests.Common;
using MechanicShop.Tests.Common.RepairTasks;

using MediatR;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Features.RepairTasks.Queries.GetRepairTaskById;

[Collection(WebAppFactoryCollection.CollectionName)]
public class GetRepairTaskByIdQueryTests(WebAppFactory factory) : BaseSubcutaneousTest(factory)
{
    private readonly WebAppFactory _factory = factory;

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnRepairTaskWithParts()
    {
        var repairTask = RepairTaskFactory.CreateRepairTask().Value;

        using (var arrangeScope = _factory.CreateScope())
        {
            var context = arrangeScope.ServiceProvider.GetRequiredService<IAppDbContext>();
            await context.RepairTasks.AddAsync(repairTask);
            await context.SaveChangesAsync(default);
        }

        var query = new GetRepairTaskByIdQuery(repairTask.Id);

        using var actScope = _factory.CreateScope();
        var mediator = actScope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.OriginalRepairTaskId.Should().Be(repairTask.Id);
        result.Value.Parts.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenRepairTaskDoesNotExist_ShouldReturnNotFoundError()
    {
        var nonExistentTaskId = Guid.CreateVersion7();
        var query = new GetRepairTaskByIdQuery(nonExistentTaskId);

        using var actScope = _factory.CreateScope();
        var mediator = actScope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(query);

        result.IsSuccess.Should().BeFalse();
        result.TopError.Code.Should().Be(Application.Common.Errors.ApplicationErrors.RepairTasks.NotFound.Code);
    }
}