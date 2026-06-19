namespace ImmoScorer.Infrastructure.AntiBot;

/// <summary>Configuration options for the anti-bot scraping strategies.</summary>
public sealed class AntiBotOptions
{
    /// <summary>Configuration section name in appsettings.json.</summary>
    public const string Section = "AntiBot";

    /// <summary>Gets or sets the minimum random delay in milliseconds between requests.</summary>
    public int MinDelayMs { get; set; } = 3000;

    /// <summary>Gets or sets the maximum random delay in milliseconds between requests.</summary>
    public int MaxDelayMs { get; set; } = 8000;

    /// <summary>Gets or sets the base backoff delay in milliseconds for exponential backoff.</summary>
    public int BaseBackoffMs { get; set; } = 2000;

    /// <summary>Gets or sets the maximum number of backoff attempts before giving up.</summary>
    public int MaxBackoffAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration in milliseconds to pause a source after captcha detection.
    /// </summary>
    public int CaptchaPauseMs { get; set; } = 300000; // 5 minutes

    /// <summary>Gets or sets the robots.txt cache duration in hours.</summary>
    public int RobotsTxtCacheHours { get; set; } = 24;
}
