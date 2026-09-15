using System.Text;

using MassTransit;

using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Features.WorkOrders.Consumers;
using MechanicShop.Domain.Identity;
using MechanicShop.Infrastructure.BackgroundJobs;
using MechanicShop.Infrastructure.Caching;
using MechanicShop.Infrastructure.Data;
using MechanicShop.Infrastructure.Data.Interceptors;
using MechanicShop.Infrastructure.Data.ReadStores;
using MechanicShop.Infrastructure.Identity;
using MechanicShop.Infrastructure.Identity.Policies;
using MechanicShop.Infrastructure.Payments.Stripe;
using MechanicShop.Infrastructure.RealTime;
using MechanicShop.Infrastructure.Services;
using MechanicShop.Infrastructure.Settings;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using StackExchange.Redis;

using Stripe;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        ArgumentNullException.ThrowIfNull(connectionString);

        // 1. Interceptors Registration
        services.AddScoped<ISaveChangesInterceptor, DomainEventInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, AuditLogInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()); // List of interceptors to be applied to the DbContext
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<ApplicationDbContextInitialiser>();

        // 2. Redis connection multiplexer (single shared instance)
        // (Fallback to L1 if Redis is absent)
        var redisConn = configuration.GetConnectionString("RedisConnection");
        bool isRedisEnabled = !string.IsNullOrWhiteSpace(redisConn);

        if (isRedisEnabled)
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var options = ConfigurationOptions.Parse(redisConn!);
                options.ConnectTimeout = 5000;
                options.ConnectRetry = 1;
                options.KeepAlive = 180;
                options.ClientName = "mechanicshop-api";
                options.AbortOnConnectFail = false; // Prevents the application from stopping if it fails to connect to Redis

                return ConnectionMultiplexer.Connect(options);
            });

            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConn;
                options.InstanceName = "MechanicShop_";
            });

            // Cache invalidation pub/sub
            services.AddSingleton<ICacheInvalidationPublisher, RedisCacheInvalidationService>();
            services.AddHostedService<RedisCacheInvalidationSubscriber>();
        }
        else
        {
            // In development mode without Docker: use a no-op publisher and do not run the Subscriber
            services.AddSingleton<ICacheInvalidationPublisher, NoOpCacheInvalidationPublisher>();
        }

        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10), // Redis L2
                LocalCacheExpiration = TimeSpan.FromSeconds(30), // Local Memory L1
            };
        });

        // 3. (Authentication & Identity)
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            var jwtSettings = configuration.GetSection("JwtSettings");

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                       Encoding.UTF8.GetBytes(jwtSettings["Secret"]!)),
            };
        });

        services.Configure<DataProtectionTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromMinutes(10);
        });

        services
        .AddIdentityCore<AppUser>(options =>
        {
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";

            options.Password.RequiredLength = 10;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = false;
            options.Password.RequiredUniqueChars = 1;
            options.SignIn.RequireConfirmedAccount = false;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IAuthorizationHandler, LaborAssignedHandler>();

        services.AddAuthorizationBuilder()
              .AddPolicy("ManagerOnly", policy => policy.RequireRole(nameof(MechanicShop.Domain.Identity.Role.Manager)))
              .AddPolicy("InventoryManagementAccess", policy =>
                    policy.RequireRole(nameof(MechanicShop.Domain.Identity.Role.Manager), nameof(MechanicShop.Domain.Identity.Role.InventoryManager)))
              .AddPolicy("PaymentProcessingAccess", policy =>
                    policy.RequireRole(nameof(MechanicShop.Domain.Identity.Role.Manager), nameof(MechanicShop.Domain.Identity.Role.Labor)))
              .AddPolicy("SelfScopedWorkOrderAccess", policy =>
                policy.Requirements.Add(new LaborAssignedRequirement()));

        services.AddTransient<IIdentityService, MechanicShop.Infrastructure.Identity.IdentityService>();

        // 4. MassTransit Transactional Outbox Setup
        services.AddMassTransit(x =>
        {
            // 1. Automatically register Consumers from the Application Layer assembly
            var applicationAssembly = typeof(SendWorkOrderCompletedEmailConsumer).Assembly;
            x.AddConsumers(applicationAssembly);

            // 2. Link the Outbox with the DB Context
            x.AddEntityFrameworkOutbox<AppDbContext>(o =>
            {
                o.UseSqlServer(); // Store and coordinate the EF Core outbox using SQL Server

                o.UseBusOutbox(); // Run the Background Worker to read and send messages

                // o.DisableInboxCleanupService();
            });

            // 3. Currently running locally in memory without RabbitMQ
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });

            // 4. Health Check
            x.ConfigureHealthCheckOptions(options =>
            {
                options.Name = "MassTransit-Bus";
            });
        });

        // 5. Remaining Services
        services.AddHealthChecks()
            .AddSqlServer(
                connectionString: connectionString,
                name: "SQL-Server",
                tags: ["ready"]);

        services.AddScoped<ITokenProvider, TokenProvider>();

        services.AddScoped<IInvoicePdfGenerator, InvoicePdfGenerator>();

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

        services.AddScoped<IWorkOrderNotifier, SignalRWorkOrderNotifier>();

        services.AddScoped<IWorkOrderScheduleReadStore, WorkOrderScheduleReadStore>();

        services.AddHostedService<OverdueBookingCleanupService>();
        services.AddHostedService<RefreshTokenCleanupBackgroundService>();

        services.Configure<StripeSettings>(configuration.GetSection(StripeSettings.SectionName));
        services.AddScoped<IStripePaymentService, StripePaymentService>();

        services.AddSingleton<IEmailTemplateRenderer, EmailTemplateRenderer>();
        services.AddTransient<IEmailSender, EmailSender>();
        services.AddTransient<ISmsSender, SmsSender>();

        StripeConfiguration.ApiKey = configuration["Stripe:SecretKey"];

        return services;
    }
}