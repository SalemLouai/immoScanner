using ImmoScorer.Domain.Scraping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace ImmoScorer.Infrastructure.AntiBot;

/// <summary>
/// Implements anti-bot protection strategies: user-agent rotation, random delays,
/// exponential backoff on errors, robots.txt compliance (with 24 h cache),
/// and captcha detection via keyword heuristics.
/// </summary>
public sealed class AntiBotService : IAntiBotService
{
    private static readonly string[] UserAgents =
    [
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_4) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:125.0) Gecko/20100101 Firefox/125.0",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 14.4; rv:125.0) Gecko/20100101 Firefox/125.0",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.0",
    ];

    private static readonly string[] CaptchaKeywords =
    [
        "captcha", "recaptcha", "hcaptcha", "robot", "automated",
        "verification required", "verify you are human", "access denied",
        "blocked", "challenge", "security check",
    ];

    private readonly AntiBotOptions _options;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AntiBotService> _logger;
    private readonly Random _random = new();

    // Per-domain error counters for backoff
    private readonly ConcurrentDictionary<string, int> _errorCounts = new();

    // robots.txt cache: domain -> (rules text, expiry)
    private readonly ConcurrentDictionary<string, (string Rules, DateTime Expiry)> _robotsCache = new();

    /// <summary>Initialises the <see cref="AntiBotService"/>.</summary>
    public AntiBotService(
        IOptions<AntiBotOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<AntiBotService> logger)
    {
        _options = options.Value;
        _httpClient = httpClientFactory.CreateClient("antibot");
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<string> GetRandomUserAgentAsync()
    {
        var ua = UserAgents[_random.Next(UserAgents.Length)];
        return Task.FromResult(ua);
    }

    /// <inheritdoc/>
    public async Task DelayBeforeRequestAsync(string domain, CancellationToken cancellationToken = default)
    {
        var errorCount = _errorCounts.GetValueOrDefault(domain, 0);
        var baseDelay = _random.Next(_options.MinDelayMs, _options.MaxDelayMs);

        // Add exponential backoff component based on prior errors
        var backoffMs = errorCount > 0
            ? (int)Math.Min(_options.BaseBackoffMs * Math.Pow(2, errorCount - 1), 60_000)
            : 0;

        var totalDelay = baseDelay + backoffMs;

        _logger.LogDebug(
            "Waiting {DelayMs} ms before request to {Domain} (errors: {ErrorCount})",
            totalDelay, domain, errorCount);

        await Task.Delay(totalDelay, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task HandleResponseErrorAsync(
        string domain,
        int statusCode,
        CancellationToken cancellationToken = default)
    {
        var newCount = _errorCounts.AddOrUpdate(domain, 1, (_, c) => c + 1);

        if (newCount > _options.MaxBackoffAttempts)
        {
            _logger.LogWarning(
                "Domain {Domain} has exceeded max backoff attempts ({MaxAttempts})",
                domain, _options.MaxBackoffAttempts);
            return;
        }

        var backoffMs = (int)Math.Min(
            _options.BaseBackoffMs * Math.Pow(2, newCount - 1), 60_000);

        _logger.LogInformation(
            "HTTP {StatusCode} from {Domain}: backoff {BackoffMs} ms (attempt {Attempt})",
            statusCode, domain, backoffMs, newCount);

        await Task.Delay(backoffMs, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> IsAllowedByRobotsTxtAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        Uri uri;
        try
        {
            uri = new Uri(url);
        }
        catch
        {
            return true; // if URL is malformed, allow (fail open)
        }

        var domain = uri.Host;
        var robotsUrl = $"{uri.Scheme}://{domain}/robots.txt";

        string rulesText;

        if (_robotsCache.TryGetValue(domain, out var cached) && cached.Expiry > DateTime.UtcNow)
        {
            rulesText = cached.Rules;
        }
        else
        {
            try
            {
                rulesText = await _httpClient.GetStringAsync(robotsUrl, cancellationToken);
                var expiry = DateTime.UtcNow.AddHours(_options.RobotsTxtCacheHours);
                _robotsCache[domain] = (rulesText, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch robots.txt from {RobotsUrl}; allowing request", robotsUrl);
                return true; // fail open
            }
        }

        // Simple robots.txt parser: check Disallow rules for "User-agent: *"
        return IsPathAllowed(rulesText, uri.PathAndQuery);
    }

    /// <inheritdoc/>
    public Task<bool> IsCaptchaDetectedAsync(string pageContent)
    {
        var lower = pageContent.ToLowerInvariant();
        var detected = CaptchaKeywords.Any(k => lower.Contains(k));
        if (detected)
        {
            _logger.LogWarning("Captcha detected in page content");
        }

        return Task.FromResult(detected);
    }

    private static bool IsPathAllowed(string robotsTxt, string path)
    {
        var lines = robotsTxt.Split('\n', StringSplitOptions.TrimEntries);
        var inRelevantBlock = false;

        foreach (var line in lines)
        {
            if (line.StartsWith("User-agent:", StringComparison.OrdinalIgnoreCase))
            {
                var agent = line["User-agent:".Length..].Trim();
                inRelevantBlock = agent == "*";
                continue;
            }

            if (!inRelevantBlock) continue;

            if (line.StartsWith("Disallow:", StringComparison.OrdinalIgnoreCase))
            {
                var disallowedPath = line["Disallow:".Length..].Trim();
                if (!string.IsNullOrEmpty(disallowedPath) &&
                    path.StartsWith(disallowedPath, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
