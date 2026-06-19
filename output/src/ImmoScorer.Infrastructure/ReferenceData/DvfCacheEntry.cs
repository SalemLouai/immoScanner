namespace ImmoScorer.Infrastructure.ReferenceData;

/// <summary>On-disk cache entry for DVF reference data.</summary>
internal sealed record DvfCacheEntry(
    string PostalCode,
    string PropertyType,
    decimal MedianPricePerM2,
    int SampleCount,
    DateTime DataAsOf,
    DateTime CachedAt);
