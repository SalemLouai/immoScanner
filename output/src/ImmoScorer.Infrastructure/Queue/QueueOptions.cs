namespace ImmoScorer.Infrastructure.Queue;

/// <summary>Configuration options for the Azure Storage Queue.</summary>
public sealed class QueueOptions
{
    /// <summary>Configuration section name in appsettings.json.</summary>
    public const string Section = "Queue";

    /// <summary>
    /// Gets or sets the Azure Storage connection string.
    /// Set via User Secrets in development; environment variable in production.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the scraping job queue.</summary>
    public string QueueName { get; set; } = "scraping-jobs";

    /// <summary>
    /// Gets or sets which queue implementation to use: "Azure" or "InMemory".
    /// Defaults to "InMemory" for local development without Azure.
    /// </summary>
    public string Provider { get; set; } = "InMemory";

    /// <summary>Gets or sets the message visibility timeout in seconds for Azure Storage Queue.</summary>
    public int VisibilityTimeoutSeconds { get; set; } = 300;
}
