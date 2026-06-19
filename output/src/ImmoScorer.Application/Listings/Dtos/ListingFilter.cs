namespace ImmoScorer.Application.Listings.Dtos;

/// <summary>
/// Optional filter parameters for the <see cref="Queries.GetListingsQuery"/>.
/// All properties are nullable; <c>null</c> means no filter applied for that dimension.
/// </summary>
public sealed record ListingFilter(
    int? MinScore,
    decimal? MaxPrice,
    decimal? MinArea,
    string? City,
    string? Source);
