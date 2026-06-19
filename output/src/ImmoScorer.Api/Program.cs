using ImmoScorer.Api.Endpoints;
using ImmoScorer.Api.Middleware;
using ImmoScorer.Application;
using ImmoScorer.Infrastructure;
using ImmoScorer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

// ── Bootstrap logger (captures startup errors before full Serilog config) ──
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) =>
    {
        cfg
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/immoscorer-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
    });

    // ── Application services ───────────────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── OpenAPI / Scalar ───────────────────────────────────────────────────
    builder.Services.AddOpenApi();

    // ── CORS ───────────────────────────────────────────────────────────────
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? ["http://localhost:5173"];

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    // ── Auto-migrate database on startup (dev / POC only) ──────────────────
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ImmoScorerDbContext>();
        await db.Database.MigrateAsync();
    }

    // ── Middleware pipeline ────────────────────────────────────────────────
    app.UseMiddleware<ErrorHandlingMiddleware>();
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseCors();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        // Scalar interactive UI at /scalar/v1
        app.MapScalarApiReference();
    }

    // ── Endpoints ──────────────────────────────────────────────────────────
    app.MapSearchEndpoints();
    app.MapListingEndpoints();

    app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
        .WithTags("Health");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Make Program accessible to WebApplicationFactory in tests
public partial class Program { }
