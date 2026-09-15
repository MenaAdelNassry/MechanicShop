using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

using Asp.Versioning;

using MechanicShop.Api.Extensions;
using MechanicShop.Api.Infrastructure;
using MechanicShop.Api.OpenApi.Transformers;
using MechanicShop.Api.Services;
using MechanicShop.Application.Common.Interfaces;
using MechanicShop.Application.Common.Settings;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        services.AddCustomProblemDetails()
                .AddCustomApiVersioning()
                .AddApiDocumentation()
                .AddExceptionHandling()
                .AddControllerWithJsonConfiguration()
                .AddConfiguredCors(configuration)
                .AddIdentityInfrastructure()
                .AddAppRateLimiting()
                .AddAppOpenTelemetry()
                .AddSignalR();

        return services;
    }

    public static IServiceCollection AddAppOpenTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
        .ConfigureResource(res => res.AddService("MechanicShop.Api"))
        .WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation().
            AddHttpClientInstrumentation();

            tracing.AddOtlpExporter();
        }).
        WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter()
            .AddPrometheusExporter() // /metrics
            .AddMeter("MechanicShop.Api"));

        return services;
    }

    public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                string partitionKey;
                int permitLimit;

                // 1. Check if the user is authenticated (logged in)
                if (context.User.Identity?.IsAuthenticated == true)
                {
                    // Use NameIdentifier (User ID) as the unique partition key
                    string userId = context.User.GetUserId().ToString();

                    partitionKey = $"user_{userId}";
                    permitLimit = 150; // Give a more generous permit limit to registered active employees/users
                }
                else
                {
                    // 2. Fallback to IP address for anonymous requests (e.g., login, register endpoints)
                    string userIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    partitionKey = $"ip_{userIp}";
                    permitLimit = 40; // Stricter limit for anonymous IPs to prevent brute-force attacks and bots
                }

                // Return the sliding window rate limiter dynamically configured based on identity
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: partitionKey,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueLimit = 10,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    });
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }

    public static IServiceCollection AddCustomProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = (context) =>
        {
            context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
            context.ProblemDetails.Extensions.Add("requestId", context.HttpContext.TraceIdentifier);
        });

        return services;
    }

    public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        string[] versions = ["v1"];

        foreach (var version in versions)
        {
            services.AddOpenApi(version, options =>
            {
                // Versioning config
                options.AddDocumentTransformer<VersionInfoTransformer>();

                // Security Scheme config
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
                options.AddOperationTransformer<BearerSecuritySchemeTransformer>();
            });
        }

        return services;
    }

    public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    public static IServiceCollection AddControllerWithJsonConfiguration(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.Converters.Add(new DecimalTwoDecimalPlacesConverter());
        });

        return services;
    }

    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUser, CurrentUser>();
        services.AddHttpContextAccessor();
        return services;
    }

    public static IServiceCollection AddConfiguredCors(this IServiceCollection services, IConfiguration configuration)
    {
        var appSettings = configuration.GetSection("AppSettings").Get<AppSettings>();

        services.AddCors(options => options.AddPolicy(
            appSettings?.CorsPolicyName ?? "DefaultCorsPolicy",
            policy =>
            {
                var origins = appSettings?.AllowedOrigins ?? [];
                if (origins.Length > 0)
                {
                    policy.WithOrigins(origins)
                          .AllowAnyHeader()
                          .AllowAnyMethod();

                    if (!origins.Contains("*"))
                    {
                        policy.AllowCredentials();
                    }
                }
            }));

        return services;
    }

    public static IApplicationBuilder UseCoreMiddlewares(this IApplicationBuilder app, IConfiguration configuration)
    {
        // 1. Exception handling should be FIRST to catch all errors
        app.UseExceptionHandler();

        // 2. Status code pages for handling HTTP status codes
        app.UseStatusCodePages();

        // 3. HTTPS redirection (before any other middleware that might generate URLs)
        app.UseHttpsRedirection();

        // 4. Serilog request logging (early to log all requests)
        app.UseSerilogRequestLogging();

        // 5. CORS (before authentication/authorization)
        app.UseCors(configuration["AppSettings:CorsPolicyName"]!);

        // 6. Authentication (must come before authorization)
        app.UseAuthentication();

        // 7. Request log context middleware (after auth to include user info in logs)
        app.UseMiddleware<RequestLogContextMiddleware>();

        // 8. Rate limiting
        app.UseRateLimiter();

        // 9. Authorization (must come after authentication)
        app.UseAuthorization();

        return app;
    }
}