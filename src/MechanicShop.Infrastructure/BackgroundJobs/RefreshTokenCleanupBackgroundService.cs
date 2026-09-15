using MechanicShop.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MechanicShop.Infrastructure.BackgroundJobs;

public sealed class RefreshTokenCleanupBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<RefreshTokenCleanupBackgroundService> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan RevokedRetentionPeriod = TimeSpan.FromDays(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RefreshToken Cleanup Service is starting with an interval of {Interval}.", Interval);

        using var timer = new PeriodicTimer(Interval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CleanUpTokensAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while cleaning up expired and revoked refresh tokens.");
            }
        }

        logger.LogInformation("RefreshToken Cleanup Service is stopping.");
    }

    private async Task CleanUpTokensAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var now = timeProvider.GetUtcNow();
        var revokedThreshold = now.Subtract(RevokedRetentionPeriod);

        var deletedCount = await context.RefreshTokens
            .Where(rt => rt.ExpiresOnUtc <= now || (rt.RevokedOnUtc != null && rt.RevokedOnUtc <= revokedThreshold))
            .ExecuteDeleteAsync(ct);

        if (deletedCount > 0)
        {
            logger.LogInformation("Cleaned up {Count} expired/revoked refresh tokens from the database.", deletedCount);
        }
    }
}