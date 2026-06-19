using ImmoScorer.Application.Common.Persistence;
using ImmoScorer.Domain.ReferenceData;
using ImmoScorer.Domain.Scraping;
using ImmoScorer.Domain.Scoring;
using ImmoScorer.Infrastructure.AntiBot;
using ImmoScorer.Infrastructure.BackgroundServices;
using ImmoScorer.Infrastructure.Persistence;
using ImmoScorer.Infrastructure.Queue;
using ImmoScorer.Infrastructure.ReferenceData;
using ImmoScorer.Infrastructure.Scoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ImmoScorer.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services into the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Wires all Infrastructure implementations: EF Core, scrapers, scoring engine,
    /// DVF client, job queue, anti-bot service, and background hosted service.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── EF Core ─────────────────────────────────────────────────────────
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=immoscorer.db";

        services.AddDbContext<ImmoScorerDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IImmoScorerDbContext>(sp =>
            sp.GetRequiredService<ImmoScorerDbContext>());

        // ── Options ──────────────────────────────────────────────────────────
        services.Configure<AntiBot.AntiBotOptions>(
            configuration.GetSection(AntiBotOptions.Section));

        services.Configure<Scraping.ScrapingOptions>(
            configuration.GetSection(Scraping.ScrapingOptions.Section));

        services.Configure<QueueOptions>(
            configuration.GetSection(QueueOptions.Section));

        services.Configure<DvfOptions>(
            configuration.GetSection(DvfOptions.Section));

        // ── HTTP Clients ─────────────────────────────────────────────────────
        services.AddHttpClient("antibot");
        services.AddHttpClient("dvf");

        // ── Anti-bot ─────────────────────────────────────────────────────────
        services.AddSingleton<IAntiBotService, AntiBot.AntiBotService>();

        // ── Scoring ──────────────────────────────────────────────────────────
        services.AddSingleton<IScoringEngine, WeightedScoringEngine>();

        // ── DVF Reference Data ───────────────────────────────────────────────
        services.AddSingleton<IDvfReferenceDataProvider, DvfDataGouvrClient>();

        // ── Job Queue (provider selected by config) ──────────────────────────
        var queueProvider = configuration[$"{QueueOptions.Section}:Provider"] ?? "InMemory";
        if (string.Equals(queueProvider, "Azure", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IScrapingJobQueue, AzureStorageQueueJobQueue>();
        }
        else
        {
            services.AddSingleton<IScrapingJobQueue, InMemoryJobQueue>();
        }

        // ── Scrapers (mode selected by config) ───────────────────────────────
        var scrapingMode = configuration[$"{Scraping.ScrapingOptions.Section}:Mode"] ?? "Fixture";
        if (string.Equals(scrapingMode, "Live", StringComparison.OrdinalIgnoreCase))
        {
            services.AddTransient<IListingScraper, Scraping.LeBonCoinScraper>();
            services.AddTransient<IListingScraper, Scraping.SeLogerScraper>();
        }
        else
        {
            // Default to fixture scraper for POC / development
            services.AddTransient<IListingScraper, Scraping.FixtureScraper>();
        }

        // ── Background Service ───────────────────────────────────────────────
        services.AddHostedService<ScrapingBackgroundService>();

        return services;
    }
}
