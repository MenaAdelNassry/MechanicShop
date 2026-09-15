namespace MechanicShop.Infrastructure.Caching;

public interface ICacheInvalidationPublisher
{
    Task PublishInvalidationAsync(string cacheKey, CancellationToken cancellationToken = default);
}
