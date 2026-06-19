using ImmoScorer.Domain.Common;
using ImmoScorer.Domain.Scraping;
using MediatR;

namespace ImmoScorer.Application.Scraping.Commands;

/// <summary>
/// Command to process a single scraping job: invokes the appropriate scraper,
/// enriches results with DVF data, scores them, and persists deduplicated listings.
/// Returns the count of newly persisted listings.
/// </summary>
public sealed record ProcessScrapingJobCommand(ScrapingJob Job)
    : IRequest<Result<int>>;
