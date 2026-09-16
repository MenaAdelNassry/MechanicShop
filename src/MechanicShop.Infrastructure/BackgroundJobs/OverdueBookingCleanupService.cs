using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Common.Settings;
using MechanicShop.Domain.Workorders.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

namespace MechanicShop.Infrastructure.BackgroundJobs;

public class OverdueBookingCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<OverdueBookingCleanupService> logger,
    IOptions<AppSettings> options,
    TimeProvider dateTime,
    IConnectionMultiplexer? multiplexer = null) : BackgroundService
{
    private readonly AppSettings _appSettings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (multiplexer is null)
        {
            logger.LogWarning("Redis is not configured. OverdueBookingCleanupService distributed lock is disabled in local mode.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_appSettings.OverdueBookingCleanupFrequencyMinutes));

        // Step 1: Get Redis database instance using the injected multiplexer
        var redisDb = multiplexer.GetDatabase();

        // Define a unique distributed key for this specific background job
        string lockKey = "lock:overdue-booking-cleanup";

        // Define a unique value representing this specific server instance
        string instanceName = Environment.MachineName;

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // Step 2 & 3: Try to acquire the distributed lock for 2 minutes
            // This is an atomic operation in Redis
            bool isLockAcquired = await redisDb.LockTakeAsync(lockKey, instanceName, TimeSpan.FromMinutes(2));

            if (!isLockAcquired)
            {
                // Lock is already held by another API instance, skip silently without flooding database
                logger.LogInformation("Another instance is already running the cleanup job. Skipping this tick safely.");
                continue;
            }

            try
            {
                // If we reached here, this instance owns the lock!
                logger.LogInformation("Lock acquired successfully by {Instance}. Processing database cleanup...", instanceName);

                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

                var cutoff = dateTime.GetUtcNow().AddMinutes(-_appSettings.BookingCancellationThresholdMinutes);
                var overdue = await db.WorkOrders
                    .Where(w => w.State == WorkOrderState.Scheduled && w.StartAtUtc <= cutoff)
                    .ToListAsync(stoppingToken);

                if (overdue.Count > 0)
                {
                    foreach (var wo in overdue)
                    {
                        var result = wo.Cancel(dateTime);
                        if (result.IsError)
                        {
                            logger.LogWarning("Failed to cancel WorkOrder {Id}: {Error}", wo.Id, result.Errors);
                        }
                    }

                    await db.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("Cancelled {Count} overdue work orders.", overdue.Count);
                }
                else
                {
                    logger.LogInformation("No overdue work orders found.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during overdue work orders cleanup execution.");
            }
            finally
            {
                // Step 4: Always release the lock after completion or failure so other instances can compete next time
                await redisDb.LockReleaseAsync(lockKey, instanceName);
                logger.LogInformation("Lock released by {Instance}.", instanceName);
            }
        }
    }
}