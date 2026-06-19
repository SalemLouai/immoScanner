using ImmoScorer.Domain.Scraping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace ImmoScorer.Infrastructure.Scraping;

/// <summary>
/// Abstract base class for Playwright-based scrapers providing shared browser lifecycle
/// management, user-agent rotation, and anti-bot delay integration.
/// </summary>
public abstract class PlaywrightScraperBase : IAsyncDisposable
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private int _pagesSinceRecycle;

    /// <summary>Gets the configured scraping options.</summary>
    protected ScrapingOptions Options { get; }

    /// <summary>Gets the anti-bot service.</summary>
    protected IAntiBotService AntiBotService { get; }

    /// <summary>Gets the logger.</summary>
    protected ILogger Logger { get; }

    /// <summary>Initialises a new instance of <see cref="PlaywrightScraperBase"/>.</summary>
    protected PlaywrightScraperBase(
        IOptions<ScrapingOptions> options,
        IAntiBotService antiBotService,
        ILogger logger)
    {
        Options = options.Value;
        AntiBotService = antiBotService;
        Logger = logger;
    }

    /// <summary>
    /// Gets or creates a Playwright page, recycling the browser after
    /// <see cref="ScrapingOptions.RecycleBrowserAfterPages"/> pages.
    /// </summary>
    protected async Task<IPage> GetPageAsync(CancellationToken cancellationToken)
    {
        if (_browser is null || _pagesSinceRecycle >= Options.RecycleBrowserAfterPages)
        {
            await RecycleBrowserAsync();
        }

        var userAgent = await AntiBotService.GetRandomUserAgentAsync();

        var context = await _browser!.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = userAgent,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            Locale = "fr-FR",
            TimezoneId = "Europe/Paris",
        });

        var page = await context.NewPageAsync();
        page.SetDefaultNavigationTimeout(Options.NavigationTimeoutMs);

        _pagesSinceRecycle++;
        return page;
    }

    private async Task RecycleBrowserAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }

        _playwright ??= await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Options.Headless,
        });

        _pagesSinceRecycle = 0;
        Logger.LogDebug("Playwright browser (re)cycled");
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }

        _playwright?.Dispose();
        _playwright = null;
    }
}
