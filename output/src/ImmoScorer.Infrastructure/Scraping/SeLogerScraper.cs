using ImmoScorer.Domain.Common;
using ImmoScorer.Domain.Enums;
using ImmoScorer.Domain.Scraping;
using ImmoScorer.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace ImmoScorer.Infrastructure.Scraping;

/// <summary>
/// Playwright-based scraper for SeLoger real-estate listings.
/// Respects robots.txt, applies UA rotation and random delays via <see cref="IAntiBotService"/>.
/// </summary>
public sealed class SeLogerScraper : PlaywrightScraperBase, IListingScraper
{
    private const string Domain = "www.seloger.com";

    /// <summary>Initialises a new instance of <see cref="SeLogerScraper"/>.</summary>
    public SeLogerScraper(
        IOptions<ScrapingOptions> options,
        IAntiBotService antiBotService,
        ILogger<SeLogerScraper> logger)
        : base(options, antiBotService, logger) { }

    /// <inheritdoc/>
    public string SourceName => "SeLoger";

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<ScrapedListing>>> ScrapeAsync(
        SearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var searchUrl = BuildSearchUrl(criteria);

        var allowed = await AntiBotService.IsAllowedByRobotsTxtAsync(searchUrl, cancellationToken);
        if (!allowed)
        {
            Logger.LogWarning("robots.txt disallows scraping {Url}", searchUrl);
            return Result<IReadOnlyList<ScrapedListing>>.Failure(
                "robots.txt disallows scraping this URL.");
        }

        await AntiBotService.DelayBeforeRequestAsync(Domain, cancellationToken);

        IPage? page = null;
        try
        {
            page = await GetPageAsync(cancellationToken);

            Logger.LogInformation("Navigating to SeLoger: {Url}", searchUrl);
            var response = await page.GotoAsync(searchUrl);

            if (response is null || !response.Ok)
            {
                var statusCode = response?.Status ?? 0;
                await AntiBotService.HandleResponseErrorAsync(Domain, statusCode, cancellationToken);
                return Result<IReadOnlyList<ScrapedListing>>.Failure(
                    $"SeLoger returned HTTP {statusCode}.");
            }

            var content = await page.ContentAsync();
            if (await AntiBotService.IsCaptchaDetectedAsync(content))
            {
                Logger.LogWarning("Captcha detected on SeLoger — pausing");
                return Result<IReadOnlyList<ScrapedListing>>.Failure(
                    "Captcha detected on SeLoger.");
            }

            var listings = await ExtractListingsAsync(page, criteria, cancellationToken);
            return Result<IReadOnlyList<ScrapedListing>>.Success(listings);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error scraping SeLoger");
            return Result<IReadOnlyList<ScrapedListing>>.Failure(
                $"Scraping error: {ex.Message}");
        }
        finally
        {
            if (page is not null)
            {
                await page.Context.CloseAsync();
            }
        }
    }

    private async Task<IReadOnlyList<ScrapedListing>> ExtractListingsAsync(
        IPage page,
        SearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        var results = new List<ScrapedListing>();

        // SeLoger uses article elements for listing cards
        var cards = await page.QuerySelectorAllAsync("article[data-id]");

        foreach (var card in cards)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var externalId = await card.GetAttributeAsync("data-id") ?? Guid.NewGuid().ToString();
                var title = await GetTextAsync(card, "h2,h3,.announcement-title");
                var priceText = await GetTextAsync(card, ".price,.announcement-price");
                var areaText = await GetTextAsync(card, ".area,.announcement-area");
                var roomsText = await GetTextAsync(card, ".rooms,.announcement-rooms");
                var linkEl = await card.QuerySelectorAsync("a");
                var url = linkEl is null ? string.Empty :
                    await linkEl.GetAttributeAsync("href") ?? string.Empty;

                if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    url = "https://www.seloger.com" + url;

                decimal.TryParse(
                    ExtractNumbers(priceText), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var price);

                decimal.TryParse(
                    ExtractNumbers(areaText), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var area);

                int.TryParse(ExtractNumbers(roomsText), out var rooms);

                results.Add(new ScrapedListing(
                    Title: title,
                    Description: null,
                    Source: SourceName,
                    City: criteria.City,
                    PostalCode: criteria.PostalCode,
                    Price: price,
                    Area: area,
                    Rooms: rooms > 0 ? rooms : null,
                    Floor: null,
                    EnergyRating: null,
                    OriginalUrl: url,
                    ExternalId: externalId));
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Skipping malformed SeLoger card");
            }
        }

        Logger.LogInformation("Extracted {Count} listings from SeLoger", results.Count);
        return results;
    }

    private static async Task<string> GetTextAsync(IElementHandle element, string selector)
    {
        var el = await element.QuerySelectorAsync(selector);
        return el is null ? string.Empty : (await el.TextContentAsync() ?? string.Empty).Trim();
    }

    private static string ExtractNumbers(string text) =>
        new string(text.Where(c => char.IsDigit(c) || c == '.').ToArray());

    private static string BuildSearchUrl(SearchCriteria criteria)
    {
        var typeCode = criteria.PropertyType switch
        {
            PropertyType.Apartment => "1",
            PropertyType.House => "2",
            _ => "1,2",
        };

        var url = $"https://www.seloger.com/list.htm?idtypebien={typeCode}" +
                  $"&cp={Uri.EscapeDataString(criteria.PostalCode)}";

        if (criteria.MinPrice.HasValue)
            url += $"&pxmin={criteria.MinPrice.Value}";
        if (criteria.MaxPrice.HasValue)
            url += $"&pxmax={criteria.MaxPrice.Value}";
        if (criteria.MinArea.HasValue)
            url += $"&surfacemin={criteria.MinArea.Value}";
        if (criteria.MaxArea.HasValue)
            url += $"&surfacemax={criteria.MaxArea.Value}";
        if (criteria.MinRooms.HasValue)
            url += $"&nb_pieces_min={criteria.MinRooms.Value}";

        return url;
    }
}
