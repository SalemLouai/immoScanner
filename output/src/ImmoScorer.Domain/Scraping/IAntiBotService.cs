namespace ImmoScorer.Domain.Scraping;

/// <summary>
/// Provides anti-bot protection strategies for web scrapers:
/// user-agent rotation, random delays, exponential backoff,
/// robots.txt compliance, and captcha detection.
/// </summary>
public interface IAntiBotService
{
    /// <summary>Returns a randomly selected User-Agent header value.</summary>
    Task<string> GetRandomUserAgentAsync();

    /// <summary>
    /// Waits a polite, randomised delay before issuing the next request to <paramref name="domain"/>.
    /// The delay escalates with prior error count for the domain.
    /// </summary>
    Task DelayBeforeRequestAsync(string domain, CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers an HTTP error response for <paramref name="domain"/> and waits the appropriate
    /// exponential backoff period before returning.
    /// </summary>
    Task HandleResponseErrorAsync(string domain, int statusCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>true</c> if the given <paramref name="url"/> is permitted to be scraped
    /// according to the domain's robots.txt (cached 24 h).
    /// </summary>
    Task<bool> IsAllowedByRobotsTxtAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns <c>true</c> if the rendered <paramref name="pageContent"/> contains
    /// signals indicating a CAPTCHA challenge page.
    /// </summary>
    Task<bool> IsCaptchaDetectedAsync(string pageContent);
}
