namespace MechanicShop.Infrastructure.Caching;

public sealed class NoOpCacheInvalidationPublisher : ICacheInvalidationPublisher
{
    public Task PublishInvalidationAsync(string tag, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}