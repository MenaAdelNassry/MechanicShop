using System.Text.Json;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MechanicShop.Infrastructure.BackgroundJobs;

public sealed class RedisCacheInvalidationSubscriber(
        IConnectionMultiplexer multiplexer,
        HybridCache hybridCache,
        ILogger<RedisCacheInvalidationSubscriber> logger) : IHostedService, IDisposable
{
    private const string ChannelName = "MechanicShop:CacheInvalidation";
    private ISubscriber? _subscriber;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _subscriber = multiplexer.GetSubscriber();

            // Subscribe and handle messages
            _subscriber.Subscribe(RedisChannel.Literal(ChannelName), (channel, value) =>
            {
                try
                {
                    string jsonString = value.ToString();

                    if (string.IsNullOrWhiteSpace(jsonString))
                    {
                        logger.LogWarning("Received empty cache invalidation payload");
                        return;
                    }

                    var doc = JsonDocument.Parse(jsonString);
                    if (doc.RootElement.TryGetProperty("Key", out var keyProp))
                    {
                        var key = keyProp.GetString();
                        if (!string.IsNullOrEmpty(key))
                        {
                            // Fire-and-Forget
                            _ = hybridCache.RemoveByTagAsync(key, cancellationToken: cancellationToken);
                            logger.LogInformation("Evicted cache key {Key} from hybrid cache (local + redis)", key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error handling cache invalidation message: {Payload}", value);
                }
            });

            logger.LogInformation("Subscribed to Redis cache invalidation channel {Channel}", ChannelName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to subscribe to Redis cache invalidation channel");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _subscriber?.Unsubscribe(RedisChannel.Literal(ChannelName));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error unsubscribing from Redis channel");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        // nothing to dispose for connection multiplexer here (singleton managed by DI)
    }
}