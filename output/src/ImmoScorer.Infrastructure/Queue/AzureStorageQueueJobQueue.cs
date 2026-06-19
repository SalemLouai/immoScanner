using System.Text.Json;
using Azure.Storage.Queues;
using ImmoScorer.Domain.Scraping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImmoScorer.Infrastructure.Queue;

/// <summary>
/// Azure Storage Queue implementation of <see cref="IScrapingJobQueue"/>.
/// Messages are serialised as JSON and encoded in Base64 as required by the SDK.
/// </summary>
public sealed class AzureStorageQueueJobQueue : IScrapingJobQueue
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly QueueClient _queueClient;
    private readonly QueueOptions _options;
    private readonly ILogger<AzureStorageQueueJobQueue> _logger;
    private bool _queueEnsured;

    /// <summary>Initialises a new instance of <see cref="AzureStorageQueueJobQueue"/>.</summary>
    public AzureStorageQueueJobQueue(
        IOptions<QueueOptions> options,
        ILogger<AzureStorageQueueJobQueue> logger)
    {
        _options = options.Value;
        _logger = logger;
        _queueClient = new QueueClient(
            _options.ConnectionString,
            _options.QueueName,
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
    }

    /// <inheritdoc/>
    public async Task EnqueueAsync(ScrapingJob job, CancellationToken cancellationToken = default)
    {
        await EnsureQueueExistsAsync(cancellationToken);

        var json = JsonSerializer.Serialize(job, JsonOptions);
        await _queueClient.SendMessageAsync(json, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Enqueued scraping job {JobId} to Azure Storage Queue {QueueName}",
            job.JobId,
            _options.QueueName);
    }

    /// <inheritdoc/>
    public async Task<ScrapingJob?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        await EnsureQueueExistsAsync(cancellationToken);

        var response = await _queueClient.ReceiveMessageAsync(
            visibilityTimeout: TimeSpan.FromSeconds(_options.VisibilityTimeoutSeconds),
            cancellationToken: cancellationToken);

        if (response?.Value is null) return null;

        var message = response.Value;

        ScrapingJob? job;
        try
        {
            job = JsonSerializer.Deserialize<ScrapingJob>(message.Body.ToString(), JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialise queue message {MessageId}", message.MessageId);
            // Delete the poison message so it doesn't block the queue
            await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
            return null;
        }

        if (job is not null)
        {
            // Delete the message after successful deserialisation (caller is responsible for processing)
            await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
        }

        return job;
    }

    private async Task EnsureQueueExistsAsync(CancellationToken cancellationToken)
    {
        if (_queueEnsured) return;
        await _queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        _queueEnsured = true;
    }
}
