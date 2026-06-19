namespace ImmoScorer.Domain.Scraping;

/// <summary>
/// Abstraction over the job queue for distributing scraping work across sources.
/// </summary>
public interface IScrapingJobQueue
{
    /// <summary>Enqueues a scraping job for later processing.</summary>
    Task EnqueueAsync(ScrapingJob job, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeues and returns the next pending scraping job, or <c>null</c> if the queue is empty.
    /// </summary>
    Task<ScrapingJob?> DequeueAsync(CancellationToken cancellationToken = default);
}
