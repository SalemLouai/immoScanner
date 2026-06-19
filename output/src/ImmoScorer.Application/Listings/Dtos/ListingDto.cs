namespace ImmoScorer.Application.Listings.Dtos;

/// <summary>Summary DTO for a listing shown in the list view.</summary>
public sealed record ListingDto(
    Guid Id,
    string Title,
    string Source,
    string City,
    string PostalCode,
    decimal Price,
    decimal Area,
    decimal PricePerM2,
    decimal ReferencePricePerM2,
    int Score,
    string OriginalUrl,
    DateTime ScrapedAt);
