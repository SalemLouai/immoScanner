using ImmoScorer.Domain.ValueObjects;

namespace ImmoScorer.Domain.Scraping;

/// <summary>
/// Message placed on the scraping job queue to distribute scraping work by source.
/// </summary>
public sealed record ScrapingJob(
    Guid JobId,
    SearchId SearchId,
    string SourceName,
    SearchCriteria Criteria);
