using MechanicShop.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public sealed class DomainEventInterceptor(
    IPublisher publisher) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine(">>> DOMAIN EVENT INTERCEPTOR: SavingChangesAsync");

        await DispatchDomainEventsAsync(
            eventData.Context,
            cancellationToken);

        return await base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }

    private async Task DispatchDomainEventsAsync(
        DbContext? context,
        CancellationToken cancellationToken)
    {
        if (context is null)
            return;

        // We keep looping as long as there is any Entity inside the Change Tracker
        // that still holds unpublished events.
        while (true)
        {
            var domainEntities = context.ChangeTracker
                .Entries<Entity>()
                .Where(e => e.Entity.DomainEvents.Count > 0)
                .Select(e => e.Entity)
                .ToList();

            if (domainEntities.Count == 0)
                break;

            var events = domainEntities
                .SelectMany(e => e.DomainEvents)
                .ToList();

            foreach (var entity in domainEntities)
            {
                entity.ClearDomainEvents();
            }

            foreach (var domainEvent in events)
            {
                await publisher.Publish(domainEvent, cancellationToken);
            }
        }
    }
}