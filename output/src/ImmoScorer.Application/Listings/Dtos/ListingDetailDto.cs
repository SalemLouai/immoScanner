namespace ImmoScorer.Application.Listings.Dtos;

/// <summary>Detailed DTO for a single listing including score breakdown and DVF reference comparison.</summary>
public sealed record ListingDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string Source,
    string City,
    string PostalCode,
    decimal Price,
    decimal Area,
    int? Rooms,
    int? Floor,
    string? EnergyRating,
    decimal PricePerM2,
    decimal ReferencePricePerM2,
    int Score,
    ScoreBreakdownDto ScoreBreakdown,
    string OriginalUrl,
    DateTime ScrapedAt);
