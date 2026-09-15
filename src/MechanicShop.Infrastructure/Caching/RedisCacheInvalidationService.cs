using System.Text.Json;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

namespace MechanicShop.Infrastructure.Caching;

public sealed class RedisCacheInvalidationService : ICacheInvalidationPublisher
{
    private const string ChannelName = "MechanicShop:CacheInvalidation";
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly ILogger<RedisCacheInvalidationService> _logger;

    public RedisCacheInvalidationService(IConnectionMultiplexer multiplexer, ILogger<RedisCacheInvalidationService> logger)
    {
        _multiplexer = multiplexer;
        _logger = logger;
    }

    public async Task PublishInvalidationAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            return;
        }

        try
        {
            var payload = JsonSerializer.Serialize(new { Key = cacheKey, When = DateTimeOffset.UtcNow });
            var sub = _multiplexer.GetSubscriber();
            await sub.PublishAsync(RedisChannel.Literal(ChannelName), payload).WaitAsync(cancellationToken);
            _logger.LogInformation("Publishing cache invalidation for key {Key} to channel {Channel}", cacheKey, ChannelName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish cache invalidation for key {Key}", cacheKey);
        }
    }
}