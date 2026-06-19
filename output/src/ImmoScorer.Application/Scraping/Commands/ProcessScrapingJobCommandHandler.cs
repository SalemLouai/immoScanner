using ImmoScorer.Application.Common.Persistence;
using ImmoScorer.Domain.Common;
using ImmoScorer.Domain.Entities;
using ImmoScorer.Domain.ReferenceData;
using ImmoScorer.Domain.Scraping;
using ImmoScorer.Domain.Scoring;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ImmoScorer.Application.Scraping.Commands;

/// <summary>
/// Handles <see cref="ProcessScrapingJobCommand"/>:
/// resolves the correct scraper, scrapes listings, enriches with DVF data,
/// scores each listing, and upserts into the database (deduplication via ExternalId).
/// </summary>
public sealed class ProcessScrapingJobCommandHandler(
    IEnumerable<IListingScraper> scrapers,
    IDvfReferenceDataProvider dvfProvider,
    IScoringEngine scoringEngine,
    IImmoScorerDbContext db,
    ILogger<ProcessScrapingJobCommandHandler> logger)
    : IRequestHandler<ProcessScrapingJobCommand, Result<int>>
{
    /// <inheritdoc/>
    public async Task<Result<int>> Handle(
        ProcessScrapingJobCommand request,
        CancellationToken cancellationToken)
    {
        var job = request.Job;

        var scraper = scrapers.FirstOrDefault(s =>
            string.Equals(s.SourceName, job.SourceName, StringComparison.OrdinalIgnoreCase));

        if (scraper is null)
        {
            return Result<int>.Failure($"No scraper found for source '{job.SourceName}'.");
        }

        logger.LogInformation(
            "Processing scraping job {JobId} for source {SourceName}, search {SearchId}",
            job.JobId,
            job.SourceName,
            job.SearchId);

        var scrapeResult = await scraper.ScrapeAsync(job.Criteria, cancellationToken);
        if (scrapeResult.IsFailure)
        {
            logger.LogWarning(
                "Scraping job {JobId} failed: {Error}",
                job.JobId,
                scrapeResult.Error);
            return Result<int>.Failure(scrapeResult.Error!);
        }

        var scrapedListings = scrapeResult.Value;
        var newCount = 0;

        foreach (var scraped in scrapedListings)
        {
            // Deduplication: skip if (Source, ExternalId) already exists
            var exists = await db.Listings
                .AnyAsync(l => l.Source == scraped.Source && l.ExternalId == scraped.ExternalId,
                    cancellationToken);

            if (exists)
            {
                logger.LogDebug(
                    "Listing {ExternalId} from {Source} already exists, skipping",
                    scraped.ExternalId,
                    scraped.Source);
                continue;
            }

            var dvfResult = await dvfProvider.GetReferenceDataAsync(
                scraped.PostalCode,
                job.Criteria.PropertyType,
                cancellationToken);

            decimal referencePricePerM2;
            if (dvfResult.IsSuccess)
            {
                referencePricePerM2 = dvfResult.Value.MedianPricePerM2;
            }
            else
            {
                logger.LogWarning(
                    "DVF data unavailable for postal code {PostalCode}: {Error}. Using 0 as reference.",
                    scraped.PostalCode,
                    dvfResult.Error);
                referencePricePerM2 = 0m;
            }

            // Build a temporary listing to compute the score (the entity has no public setters)
            var tempListing = Listing.Create(
                searchId: job.SearchId,
                scraped: scraped,
                referencePricePerM2: referencePricePerM2,
                score: new Domain.ValueObjects.Score(0),
                breakdown: new Domain.ValueObjects.ScoreBreakdown(0, 0, 0, 0));

            var score = scoringEngine.ComputeScore(tempListing, dvfResult.IsSuccess
                ? dvfResult.Value
                : new Domain.ValueObjects.DvfReferenceData(
                    scraped.PostalCode,
                    job.Criteria.PropertyType,
                    0m,
                    0,
                    DateTime.UtcNow));

            var breakdown = scoringEngine.ComputeBreakdown(tempListing, dvfResult.IsSuccess
                ? dvfResult.Value
                : new Domain.ValueObjects.DvfReferenceData(
                    scraped.PostalCode,
                    job.Criteria.PropertyType,
                    0m,
                    0,
                    DateTime.UtcNow));

            var listing = Listing.Create(
                searchId: job.SearchId,
                scraped: scraped,
                referencePricePerM2: referencePricePerM2,
                score: score,
                breakdown: breakdown);

            db.Listings.Add(listing);
            newCount++;
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Scraping job {JobId} completed: {NewCount} new listings persisted",
            job.JobId,
            newCount);

        return Result<int>.Success(newCount);
    }
}
