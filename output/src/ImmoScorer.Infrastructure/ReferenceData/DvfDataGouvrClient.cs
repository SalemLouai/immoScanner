using System.Text.Json;
using ImmoScorer.Domain.Common;
using ImmoScorer.Domain.Enums;
using ImmoScorer.Domain.ReferenceData;
using ImmoScorer.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImmoScorer.Infrastructure.ReferenceData;

/// <summary>
/// Fetches DVF (Demande de Valeurs Foncières) reference price data from data.gouv.fr.
/// Results are cached locally as JSON files to avoid repeated large downloads.
/// </summary>
public sealed class DvfDataGouvrClient : IDvfReferenceDataProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly HttpClient _httpClient;
    private readonly DvfOptions _options;
    private readonly ILogger<DvfDataGouvrClient> _logger;

    // In-memory cache for this session
    private readonly Dictionary<string, DvfReferenceData> _memoryCache = [];

    /// <summary>Initialises a new instance of <see cref="DvfDataGouvrClient"/>.</summary>
    public DvfDataGouvrClient(
        IHttpClientFactory httpClientFactory,
        IOptions<DvfOptions> options,
        ILogger<DvfDataGouvrClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("dvf");
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<DvfReferenceData>> GetReferenceDataAsync(
        string postalCode,
        PropertyType propertyType,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{postalCode}_{propertyType}";

        if (_memoryCache.TryGetValue(cacheKey, out var cached))
        {
            return Result<DvfReferenceData>.Success(cached);
        }

        // Try disk cache
        var diskResult = await TryLoadFromDiskCacheAsync(postalCode, propertyType, cancellationToken);
        if (diskResult is not null)
        {
            _memoryCache[cacheKey] = diskResult;
            return Result<DvfReferenceData>.Success(diskResult);
        }

        // Fetch from API
        return await FetchAndCacheAsync(postalCode, propertyType, cacheKey, cancellationToken);
    }

    private async Task<DvfReferenceData?> TryLoadFromDiskCacheAsync(
        string postalCode,
        PropertyType propertyType,
        CancellationToken cancellationToken)
    {
        var path = GetCachePath(postalCode, propertyType);
        if (!File.Exists(path)) return null;

        try
        {
            await using var stream = File.OpenRead(path);
            var entry = await JsonSerializer.DeserializeAsync<DvfCacheEntry>(
                stream, JsonOptions, cancellationToken);

            if (entry is null) return null;

            if (DateTime.UtcNow - entry.CachedAt > TimeSpan.FromDays(_options.CacheTtlDays))
            {
                _logger.LogDebug("DVF cache expired for {PostalCode}/{PropertyType}", postalCode, propertyType);
                return null;
            }

            return new DvfReferenceData(
                entry.PostalCode,
                propertyType,
                entry.MedianPricePerM2,
                entry.SampleCount,
                entry.DataAsOf);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read DVF disk cache at {Path}", path);
            return null;
        }
    }

    private async Task<Result<DvfReferenceData>> FetchAndCacheAsync(
        string postalCode,
        PropertyType propertyType,
        string cacheKey,
        CancellationToken cancellationToken)
    {
        try
        {
            // Use the DVF GeoAPI endpoint which returns aggregated stats by code postal
            var department = postalCode.Length >= 2 ? postalCode[..2] : postalCode;
            var typeLocal = propertyType switch
            {
                PropertyType.Apartment => "Appartement",
                PropertyType.House => "Maison",
                _ => "Appartement",
            };

            var url = $"{_options.BaseUrl.TrimEnd('/')}/?code_postal={postalCode}&type_local={Uri.EscapeDataString(typeLocal)}&page_size=500";

            _logger.LogInformation(
                "Fetching DVF data from {Url} for {PostalCode}/{PropertyType}",
                url, postalCode, propertyType);

            _ = department; // used for logging if needed

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "DVF API returned {StatusCode} for {PostalCode}",
                    response.StatusCode, postalCode);
                return Result<DvfReferenceData>.Failure(
                    $"DVF API returned {(int)response.StatusCode}.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var (medianPrice, sampleCount) = ParseDvfResponse(doc);

            if (sampleCount == 0)
            {
                return Result<DvfReferenceData>.Failure(
                    $"No DVF records found for {postalCode} / {propertyType}.");
            }

            var data = new DvfReferenceData(
                postalCode,
                propertyType,
                medianPrice,
                sampleCount,
                DateTime.UtcNow);

            _memoryCache[cacheKey] = data;
            await SaveToDiskCacheAsync(data, cancellationToken);

            return Result<DvfReferenceData>.Success(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching DVF data for {PostalCode}", postalCode);
            return Result<DvfReferenceData>.Failure($"DVF fetch error: {ex.Message}");
        }
    }

    private static (decimal MedianPrice, int Count) ParseDvfResponse(JsonDocument doc)
    {
        var root = doc.RootElement;

        if (!root.TryGetProperty("results", out var results))
            return (0m, 0);

        var prices = new List<decimal>();

        foreach (var item in results.EnumerateArray())
        {
            if (item.TryGetProperty("valeur_fonciere", out var vf) &&
                item.TryGetProperty("surface_reelle_bati", out var surf))
            {
                if (vf.TryGetDecimal(out var price) &&
                    surf.TryGetDecimal(out var surface) &&
                    surface > 0)
                {
                    prices.Add(price / surface);
                }
            }
        }

        if (prices.Count == 0) return (0m, 0);

        prices.Sort();
        var median = prices[prices.Count / 2];
        return (median, prices.Count);
    }

    private async Task SaveToDiskCacheAsync(
        DvfReferenceData data,
        CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(_options.CacheDirectory);
            var path = GetCachePath(data.PostalCode, data.PropertyType);

            var entry = new DvfCacheEntry(
                data.PostalCode,
                data.PropertyType.ToString(),
                data.MedianPricePerM2,
                data.SampleCount,
                data.DataAsOf,
                DateTime.UtcNow);

            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, entry, JsonOptions, cancellationToken);

            _logger.LogDebug("DVF cache written to {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write DVF disk cache");
        }
    }

    private string GetCachePath(string postalCode, PropertyType propertyType) =>
        Path.Combine(_options.CacheDirectory, $"dvf_{postalCode}_{propertyType}.json");
}
