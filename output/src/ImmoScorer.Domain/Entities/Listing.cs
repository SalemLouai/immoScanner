using ImmoScorer.Domain.Scraping;
using ImmoScorer.Domain.ValueObjects;

namespace ImmoScorer.Domain.Entities;

/// <summary>
/// Represents a real-estate listing scraped from a source, enriched with DVF data and scored.
/// </summary>
public sealed class Listing
{
    /// <summary>EF Core constructor.</summary>
    private Listing() { }

    /// <summary>
    /// Creates a new <see cref="Listing"/> from a scraped listing after DVF enrichment and scoring.
    /// </summary>
    public static Listing Create(
        SearchId searchId,
        ScrapedListing scraped,
        decimal referencePricePerM2,
        Score score,
        ScoreBreakdown breakdown)
    {
        var pricePerM2 = scraped.Area > 0 ? scraped.Price / scraped.Area : 0m;

        return new Listing
        {
            Id = ListingId.New(),
            SearchId = searchId,
            Title = scraped.Title,
            Description = scraped.Description,
            Source = scraped.Source,
            Address = new Address(scraped.City, scraped.PostalCode, null),
            Price = new Price(scraped.Price),
            Area = scraped.Area,
            Rooms = scraped.Rooms,
            Floor = scraped.Floor,
            EnergyRating = scraped.EnergyRating,
            PricePerM2 = pricePerM2,
            ReferencePricePerM2 = referencePricePerM2,
            Score = score,
            ScoreBreakdown = breakdown,
            OriginalUrl = scraped.OriginalUrl,
            ExternalId = scraped.ExternalId,
            ScrapedAt = DateTime.UtcNow,
        };
    }

    /// <summary>Gets the unique identifier for this listing.</summary>
    public ListingId Id { get; private set; }

    /// <summary>Gets the identifier of the search this listing belongs to.</summary>
    public SearchId SearchId { get; private set; }

    /// <summary>Gets the listing title.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Gets the full listing description, if available.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the name of the data source (e.g. "LeBonCoin", "SeLoger").</summary>
    public string Source { get; private set; } = string.Empty;

    /// <summary>Gets the address of the property.</summary>
    public Address Address { get; private set; } = null!;

    /// <summary>Gets the listed price.</summary>
    public Price Price { get; private set; } = null!;

    /// <summary>Gets the area in m².</summary>
    public decimal Area { get; private set; }

    /// <summary>Gets the number of rooms, if known.</summary>
    public int? Rooms { get; private set; }

    /// <summary>Gets the floor level, if known.</summary>
    public int? Floor { get; private set; }

    /// <summary>Gets the DPE energy rating (A–G), if known.</summary>
    public string? EnergyRating { get; private set; }

    /// <summary>Gets the computed price per m² (Price.Amount / Area).</summary>
    public decimal PricePerM2 { get; private set; }

    /// <summary>Gets the DVF reference price per m² for this postal code and property type.</summary>
    public decimal ReferencePricePerM2 { get; private set; }

    /// <summary>Gets the opportunity score.</summary>
    public Score Score { get; private set; } = null!;

    /// <summary>Gets the score breakdown showing contribution of each factor.</summary>
    public ScoreBreakdown ScoreBreakdown { get; private set; } = null!;

    /// <summary>Gets the original URL of the listing on the source website.</summary>
    public string OriginalUrl { get; private set; } = string.Empty;

    /// <summary>Gets the external identifier from the source website (used for deduplication).</summary>
    public string ExternalId { get; private set; } = string.Empty;

    /// <summary>Gets the UTC timestamp when this listing was scraped.</summary>
    public DateTime ScrapedAt { get; private set; }
}
