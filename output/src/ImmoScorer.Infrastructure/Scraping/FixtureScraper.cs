using ImmoScorer.Domain.Common;
using ImmoScorer.Domain.Scraping;
using ImmoScorer.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ImmoScorer.Infrastructure.Scraping;

/// <summary>
/// Fallback scraper that returns static fixture listings.
/// Used in development or when live scraping is blocked or ethically questionable.
/// </summary>
public sealed class FixtureScraper : IListingScraper
{
    private readonly ILogger<FixtureScraper> _logger;

    /// <summary>Initialises a new instance of <see cref="FixtureScraper"/>.</summary>
    public FixtureScraper(ILogger<FixtureScraper> logger) => _logger = logger;

    /// <inheritdoc/>
    public string SourceName => "Fixture";

    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<ScrapedListing>>> ScrapeAsync(
        SearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "FixtureScraper returning static data for {City} ({PostalCode})",
            criteria.City, criteria.PostalCode);

        IReadOnlyList<ScrapedListing> listings =
        [
            new ScrapedListing(
                Title: "Appartement 3 pièces avec balcon",
                Description: "Beau T3 lumineux en centre-ville, double vitrage, parquet.",
                Source: SourceName,
                City: criteria.City,
                PostalCode: criteria.PostalCode,
                Price: 220_000m,
                Area: 65m,
                Rooms: 3,
                Floor: 2,
                EnergyRating: "C",
                OriginalUrl: "https://fixture.example.com/ad/1",
                ExternalId: "fixture-001"),

            new ScrapedListing(
                Title: "Maison 4 pièces jardin",
                Description: "Maison de ville avec jardin privatif, 4 chambres, garage.",
                Source: SourceName,
                City: criteria.City,
                PostalCode: criteria.PostalCode,
                Price: 380_000m,
                Area: 110m,
                Rooms: 4,
                Floor: 0,
                EnergyRating: "D",
                OriginalUrl: "https://fixture.example.com/ad/2",
                ExternalId: "fixture-002"),

            new ScrapedListing(
                Title: "Studio refait à neuf",
                Description: "Studio de 28m² entièrement rénové, proche transports.",
                Source: SourceName,
                City: criteria.City,
                PostalCode: criteria.PostalCode,
                Price: 110_000m,
                Area: 28m,
                Rooms: 1,
                Floor: 4,
                EnergyRating: "B",
                OriginalUrl: "https://fixture.example.com/ad/3",
                ExternalId: "fixture-003"),

            new ScrapedListing(
                Title: "Grand appartement haussmannien",
                Description: "T5 de 145m² avec parquet d'époque, moulures, cave et parking.",
                Source: SourceName,
                City: criteria.City,
                PostalCode: criteria.PostalCode,
                Price: 650_000m,
                Area: 145m,
                Rooms: 5,
                Floor: 3,
                EnergyRating: "E",
                OriginalUrl: "https://fixture.example.com/ad/4",
                ExternalId: "fixture-004"),

            new ScrapedListing(
                Title: "Appartement 2 pièces vue dégagée",
                Description: "T2 au 8ème étage avec ascenseur, vue panoramique.",
                Source: SourceName,
                City: criteria.City,
                PostalCode: criteria.PostalCode,
                Price: 185_000m,
                Area: 48m,
                Rooms: 2,
                Floor: 8,
                EnergyRating: "A",
                OriginalUrl: "https://fixture.example.com/ad/5",
                ExternalId: "fixture-005"),
        ];

        return Task.FromResult(Result<IReadOnlyList<ScrapedListing>>.Success(listings));
    }
}
