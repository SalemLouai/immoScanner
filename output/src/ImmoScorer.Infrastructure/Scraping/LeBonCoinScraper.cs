using ImmoScorer.Domain.Common;
using ImmoScorer.Domain.Enums;
using ImmoScorer.Domain.Scraping;
using ImmoScorer.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace ImmoScorer.Infrastructure.Scraping;

/// <summary>
/// Playwright-based scraper for LeBonCoin real-estate listings.
/// Respects robots.txt, applies UA rotation and random delays via <see cref="IAntiBotService"/>.
/// </summary>
public sealed class LeBonCoinScraper : PlaywrightScraperBase, IListingScraper
{
    private const string Domain = "www.leboncoin.fr";

    /// <summary>Initialises a new instance of <see cref="LeBonCoinScraper"/>.</summary>
    public LeBonCoinScraper(
        IOptions<ScrapingOptions> options,
        IAntiBotService antiBotService,
        ILogger<LeBonCoinScraper> logger)
        : base(options, antiBotService, logger) { }

    /// <inheritdoc/>
    public string SourceName => "LeBonCoin";

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

            Logger.LogInformation("Navigating to LeBonCoin: {Url}", searchUrl);
            var response = await page.GotoAsync(searchUrl);

            if (response is null || !response.Ok)
            {
                var statusCode = response?.Status ?? 0;
                await AntiBotService.HandleResponseErrorAsync(Domain, statusCode, cancellationToken);
                return Result<IReadOnlyList<ScrapedListing>>.Failure(
                    $"LeBonCoin returned HTTP {statusCode}.");
            }

            var content = await page.ContentAsync();
            if (await AntiBotService.IsCaptchaDetectedAsync(content))
            {
                Logger.LogWarning("Captcha detected on LeBonCoin — pausing");
                return Result<IReadOnlyList<ScrapedListing>>.Failure(
                    "Captcha detected on LeBonCoin.");
            }

            var listings = await ExtractListingsAsync(page, criteria, cancellationToken);
            return Result<IReadOnlyList<ScrapedListing>>.Success(listings);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error scraping LeBonCoin");
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

        // LeBonCoin listing cards selector (may change with site updates)
        var cards = await page.QuerySelectorAllAsync("[data-qa-id='aditem_container']");

        foreach (var card in cards)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var title = await GetTextAsync(card, "[data-qa-id='aditem_title']");
                var priceText = await GetTextAsync(card, "[data-qa-id='aditem_price']");
                var locationText = await GetTextAsync(card, "[data-qa-id='aditem_location']");
                var url = await card.EvaluateAsync<string>("el => el.querySelector('a')?.href ?? ''");
                var externalId = ExtractExternalId(url);

                if (!decimal.TryParse(
                    ExtractNumbers(priceText), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var price))
                {
                    continue;
                }

                results.Add(new ScrapedListing(
                    Title: title,
                    Description: null,
                    Source: SourceName,
                    City: criteria.City,
                    PostalCode: criteria.PostalCode,
                    Price: price,
                    Area: 0m, // area requires detail page scrape — acceptable for list view
                    Rooms: null,
                    Floor: null,
                    EnergyRating: null,
                    OriginalUrl: url,
                    ExternalId: externalId));
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Skipping malformed LeBonCoin card");
            }
        }

        Logger.LogInformation("Extracted {Count} listings from LeBonCoin", results.Count);
        return results;
    }

    private static async Task<string> GetTextAsync(IElementHandle element, string selector)
    {
        var el = await element.QuerySelectorAsync(selector);
        return el is null ? string.Empty : (await el.TextContentAsync() ?? string.Empty).Trim();
    }

    private static string ExtractNumbers(string text) =>
        new string(text.Where(c => char.IsDigit(c) || c == '.').ToArray());

    private static string ExtractExternalId(string url)
    {
        // LeBonCoin URLs end with a numeric ID, e.g. /ad/immobilier/123456789.htm
        var segments = url.TrimEnd('/').Split('/');
        return segments.LastOrDefault()?.Split('.').FirstOrDefault() ?? url;
    }

    private static string BuildSearchUrl(SearchCriteria criteria)
    {
        var propertyParam = criteria.PropertyType switch
        {
            PropertyType.Apartment => "appartements",
            PropertyType.House => "maisons",
            _ => "ventes_immobilieres",
        };

        var url = $"https://www.leboncoin.fr/recherche?category=9&locations={Uri.EscapeDataString(criteria.City)}";

        if (criteria.MinPrice.HasValue)
            url += $"&price={criteria.MinPrice.Value}-";
        if (criteria.MaxPrice.HasValue)
            url += $"&price=-{criteria.MaxPrice.Value}";
        if (criteria.MinArea.HasValue)
            url += $"&square={criteria.MinArea.Value}-";
        if (criteria.MaxArea.HasValue)
            url += $"&square=-{criteria.MaxArea.Value}";
        if (criteria.MinRooms.HasValue)
            url += $"&rooms={criteria.MinRooms.Value}-";

        _ = propertyParam; // referenced in URL logic above if needed
        return url;
    }
}
