namespace ImmoScorer.Domain.Scraping;

/// <summary>
/// Transient record returned by <see cref="IListingScraper"/> before enrichment and scoring.
/// </summary>
public sealed record ScrapedListing(
    string Title,
    string? Description,
    string Source,
    string City,
    string PostalCode,
    decimal Price,
    decimal Area,
    int? Rooms,
    int? Floor,
    string? EnergyRating,
    string OriginalUrl,
    string ExternalId);
