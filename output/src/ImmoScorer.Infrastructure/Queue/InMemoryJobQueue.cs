using System.Collections.Concurrent;
using ImmoScorer.Domain.Scraping;

namespace ImmoScorer.Infrastructure.Queue;

/// <summary>
/// In-memory implementation of <see cref="IScrapingJobQueue"/> for local development
/// without an Azure Storage Account.
/// </summary>
public sealed class InMemoryJobQueue : IScrapingJobQueue
{
    private readonly ConcurrentQueue<ScrapingJob> _queue = new();

    /// <inheritdoc/>
    public Task EnqueueAsync(ScrapingJob job, CancellationToken cancellationToken = default)
    {
        _queue.Enqueue(job);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<ScrapingJob?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        _queue.TryDequeue(out var job);
        return Task.FromResult(job);
    }
}
