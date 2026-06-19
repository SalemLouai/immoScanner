using ImmoScorer.Domain.Common;
using ImmoScorer.Domain.ValueObjects;

namespace ImmoScorer.Domain.Scraping;

/// <summary>
/// Abstraction for scraping listings from a real-estate source.
/// One implementation per source (LeBonCoin, SeLoger, Fixture).
/// </summary>
public interface IListingScraper
{
    /// <summary>Gets the unique name of the data source (e.g. "LeBonCoin", "SeLoger").</summary>
    string SourceName { get; }

    /// <summary>
    /// Scrapes listings matching <paramref name="criteria"/> from this source.
    /// </summary>
    Task<Result<IReadOnlyList<ScrapedListing>>> ScrapeAsync(
        SearchCriteria criteria,
        CancellationToken cancellationToken = default);
}
