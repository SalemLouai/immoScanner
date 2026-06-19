using ImmoScorer.Domain.Enums;

namespace ImmoScorer.Domain.ValueObjects;

/// <summary>
/// Encapsulates the criteria used when running a real-estate search.
/// </summary>
public sealed record SearchCriteria(
    string City,
    string PostalCode,
    PropertyType PropertyType,
    decimal? MinPrice,
    decimal? MaxPrice,
    decimal? MinArea,
    decimal? MaxArea,
    int? MinRooms,
    int? MaxRooms);
