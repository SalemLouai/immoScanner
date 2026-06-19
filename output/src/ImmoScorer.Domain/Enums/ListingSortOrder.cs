namespace ImmoScorer.Domain.Enums;

/// <summary>Sort order for listing queries.</summary>
public enum ListingSortOrder
{
    /// <summary>Sort by opportunity score, highest first.</summary>
    ScoreDescending,

    /// <summary>Sort by price, lowest first.</summary>
    PriceAscending,

    /// <summary>Sort by price, highest first.</summary>
    PriceDescending,

    /// <summary>Sort by price per m², lowest first.</summary>
    PricePerM2Ascending,

    /// <summary>Sort by scraping date, most recent first.</summary>
    DateDescending,
}
