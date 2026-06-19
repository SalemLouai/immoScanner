using ImmoScorer.Domain.Enums;

namespace ImmoScorer.Application.Searches.Dtos;

/// <summary>Summary DTO for a saved search shown in the search list.</summary>
public sealed record SavedSearchDto(
    Guid Id,
    string City,
    string PostalCode,
    PropertyType PropertyType,
    string Status,
    int ListingCount,
    DateTime CreatedAt,
    DateTime? CompletedAt);
