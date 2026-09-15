using MechanicShop.Infrastructure.Data;
using MechanicShop.Infrastructure.RealTime;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;

using Scalar.AspNetCore;

using Serilog;

Console.WriteLine(">>> [STARTUP 1/8] Starting WebApplication Builder...");
var builder = WebApplication.CreateBuilder(args);

Console.WriteLine(">>> [STARTUP 2/8] Registering Presentation Layer...");
builder.Services.AddPresentation(builder.Configuration);

Console.WriteLine(">>> [STARTUP 3/8] Registering Application Layer...");
builder.Services.AddApplication();

Console.WriteLine(">>> [STARTUP 4/8] Registering Infrastructure Layer...");
builder.Services.AddInfrastructure(builder.Configuration);

builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

Console.WriteLine(">>> [STARTUP 5/8] Building App (builder.Build)...");
var app = builder.Build();
Console.WriteLine(">>> [STARTUP 5/8 SUCCESS] App Built successfully!");

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "MechanicShop API V1");
        options.EnableDeepLinking();
        options.DisplayRequestDuration();
        options.EnableFilter();
    });

    app.MapScalarApiReference();
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseHsts();
}

Console.WriteLine(">>> [STARTUP 6/8] Configuring Middlewares...");
app.UseCoreMiddlewares(builder.Configuration);

Console.WriteLine(">>> [STARTUP 7/8] Mapping Endpoints...");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapControllers();
app.MapStaticAssets();
app.MapHub<WorkOrderHub>("/hubs/workorders");
app.MapPrometheusScrapingEndpoint();

Console.WriteLine(">>> [STARTUP 7.5/8] Initialising & Seeding Database...");
await app.InitialiseDatabaseAsync();
Console.WriteLine(">>> [STARTUP 7.5/8 SUCCESS] Database Initialised Successfully!");

Console.WriteLine(">>> [STARTUP 8/8] Starting Server (app.Run)...");
Console.WriteLine("==================================================");
Console.WriteLine(">>> REGISTERED HOSTED SERVICES (EXECUTION ORDER):");
var hostedServices = app.Services.GetServices<IHostedService>();
foreach (var service in hostedServices)
{
    Console.WriteLine($" -> {service.GetType().Name}");
}

Console.WriteLine("==================================================");
app.Run();