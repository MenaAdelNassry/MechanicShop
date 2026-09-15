using MechanicShop.Api;
using MechanicShop.Application.Common.Settings;
using MechanicShop.Domain.Workorders;
using MechanicShop.Infrastructure.BackgroundJobs;
using MechanicShop.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.MsSql;
using Xunit;

namespace MechanicShop.Application.SubcutaneousTests.Common;

public class WebAppFactory : WebApplicationFactory<IAssemblyMarker>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public IServiceScope CreateScope()
    {
        // Return a DI scope only. Do NOT perform database cleanup here.
        // Tests call CreateScope() multiple times inside a single test (arrange -> assert),
        // so cleaning inside CreateScope() will delete rows created earlier in the same test.
        return Services.CreateScope();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Remove Distributed Cache entries to avoid stale data after DB reset
        var hybridCache = scope.ServiceProvider.GetService<HybridCache>();
        if (hybridCache != null)
        {
            var tagsToEvict = new[]
            {
                "customer",
                "invoice",
                "work-order",
                "labors",
                "repair-tasks",
                "repair-task"
            };

            foreach (var tag in tagsToEvict)
            {
                await hybridCache.RemoveByTagAsync(tag);
            }
        }

        // Remove all rows in DB from all tables in the correct order to avoid foreign key constraint violations
        context.RefreshTokens.RemoveRange(context.RefreshTokens);
        context.Users.RemoveRange(context.Users);

        context.Invoices.RemoveRange(context.Invoices);
        context.Set<WorkOrderTask>().RemoveRange(context.Set<WorkOrderTask>());
        context.WorkOrders.RemoveRange(context.WorkOrders);
        context.Vehicles.RemoveRange(context.Vehicles);
        context.Customers.RemoveRange(context.Customers);
        context.Employees.RemoveRange(context.Employees);
        context.RepairTasks.RemoveRange(context.RepairTasks);
        context.InventoryItems.RemoveRange(context.InventoryItems);
        context.InventoryTransactions.RemoveRange(context.InventoryTransactions);
        context.RepairTaskParts.RemoveRange(context.RepairTaskParts);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync();

        context.Invoices.RemoveRange(context.Invoices);
        context.WorkOrders.RemoveRange(context.WorkOrders);
        context.Vehicles.RemoveRange(context.Vehicles);
        context.Customers.RemoveRange(context.Customers);
        context.Employees.RemoveRange(context.Employees);
        context.RepairTasks.RemoveRange(context.RepairTasks);
        context.InventoryItems.RemoveRange(context.InventoryItems);
        context.InventoryTransactions.RemoveRange(context.InventoryTransactions);
        context.RepairTaskParts.RemoveRange(context.RepairTaskParts);

        await context.SaveChangesAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Avoid Development seeding (month of work orders) which hangs test startup.
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var basePath = AppContext.BaseDirectory;
            config.SetBasePath(basePath);
            config.AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: false);
        });

        builder.ConfigureServices((context, services) =>
        {
            services.Configure<AppSettings>(
                context.Configuration.GetSection("AppSettings"));
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<OverdueBookingCleanupService>();

            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.UseSqlServer(_dbContainer.GetConnectionString());
            });
        });
    }
}