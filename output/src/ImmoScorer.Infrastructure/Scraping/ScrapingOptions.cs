namespace ImmoScorer.Infrastructure.Scraping;

/// <summary>Configuration options for the Playwright-based scrapers.</summary>
public sealed class ScrapingOptions
{
    /// <summary>Configuration section name in appsettings.json.</summary>
    public const string Section = "Scraping";

    /// <summary>Gets or sets whether to run Playwright in headless mode (default: true).</summary>
    public bool Headless { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of pages to scrape before recycling the browser
    /// instance to avoid fingerprint accumulation.
    /// </summary>
    public int RecycleBrowserAfterPages { get; set; } = 20;

    /// <summary>Gets or sets the default navigation timeout in milliseconds.</summary>
    public int NavigationTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets which scraper implementation to use: "Live" or "Fixture".
    /// When set to "Fixture", the <see cref="FixtureScraper"/> is used instead of live scrapers.
    /// </summary>
    public string Mode { get; set; } = "Fixture";
}
