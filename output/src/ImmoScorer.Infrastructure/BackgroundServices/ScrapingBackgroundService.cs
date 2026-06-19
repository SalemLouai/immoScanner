using ImmoScorer.Application.Scraping.Commands;
using ImmoScorer.Domain.Scraping;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ImmoScorer.Infrastructure.BackgroundServices;

/// <summary>
/// Hosted service that continuously polls the scraping job queue and dispatches
/// <see cref="ProcessScrapingJobCommand"/> for each dequeued job.
/// </summary>
public sealed class ScrapingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScrapingBackgroundService> _logger;

    /// <summary>Initialises a new instance of <see cref="ScrapingBackgroundService"/>.</summary>
    public ScrapingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ScrapingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scraping background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var queue = scope.ServiceProvider.GetRequiredService<IScrapingJobQueue>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var job = await queue.DequeueAsync(stoppingToken);

                if (job is not null)
                {
                    _logger.LogInformation(
                        "Processing scraping job {JobId} for source {SourceName}",
                        job.JobId,
                        job.SourceName);

                    var result = await mediator.Send(
                        new ProcessScrapingJobCommand(job),
                        stoppingToken);

                    if (result.IsFailure)
                    {
                        _logger.LogWarning(
                            "Scraping job {JobId} failed: {Error}",
                            job.JobId,
                            result.Error);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Scraping job {JobId} completed with {Count} new listings",
                            job.JobId,
                            result.Value);
                    }
                }
                else
                {
                    // No jobs — wait before polling again
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in scraping background service");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("Scraping background service stopped");
    }
}
