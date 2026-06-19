using ImmoScorer.Application.Common.Persistence;
using ImmoScorer.Domain.Common;
using ImmoScorer.Domain.Scraping;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ImmoScorer.Application.Scraping.Commands;

/// <summary>
/// Handles <see cref="TriggerScrapingCommand"/>:
/// finds the search, marks it in-progress, and enqueues one scraping job per available source.
/// </summary>
public sealed class TriggerScrapingCommandHandler(
    IImmoScorerDbContext db,
    IScrapingJobQueue jobQueue,
    IEnumerable<IListingScraper> scrapers,
    ILogger<TriggerScrapingCommandHandler> logger)
    : IRequestHandler<TriggerScrapingCommand, Result<Unit>>
{
    /// <inheritdoc/>
    public async Task<Result<Unit>> Handle(
        TriggerScrapingCommand request,
        CancellationToken cancellationToken)
    {
        var search = await db.Searches
            .FirstOrDefaultAsync(s => s.Id == request.SearchId, cancellationToken);

        if (search is null)
        {
            return Result<Unit>.Failure($"Search {request.SearchId} not found.");
        }

        search.MarkInProgress();
        await db.SaveChangesAsync(cancellationToken);

        foreach (var scraper in scrapers)
        {
            var job = new ScrapingJob(
                JobId: Guid.NewGuid(),
                SearchId: request.SearchId,
                SourceName: scraper.SourceName,
                Criteria: search.Criteria);

            await jobQueue.EnqueueAsync(job, cancellationToken);

            logger.LogInformation(
                "Scraping job {JobId} enqueued for source {SourceName} on search {SearchId}",
                job.JobId,
                job.SourceName,
                job.SearchId);
        }

        return Result<Unit>.Success(Unit.Value);
    }
}
