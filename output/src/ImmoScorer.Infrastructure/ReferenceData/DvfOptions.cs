namespace ImmoScorer.Infrastructure.ReferenceData;

/// <summary>Configuration options for the DVF data client.</summary>
public sealed class DvfOptions
{
    /// <summary>Configuration section name in appsettings.json.</summary>
    public const string Section = "Dvf";

    /// <summary>Gets or sets the base URL for the DVF API on data.gouv.fr.</summary>
    public string BaseUrl { get; set; } = "https://api.dvf.etalab.gouv.fr/geoapi/dvf/";

    /// <summary>Gets or sets the local directory path for the JSON file cache.</summary>
    public string CacheDirectory { get; set; } = "./dvf-cache";

    /// <summary>Gets or sets how many days to keep cached DVF data before refreshing.</summary>
    public int CacheTtlDays { get; set; } = 90;
}
