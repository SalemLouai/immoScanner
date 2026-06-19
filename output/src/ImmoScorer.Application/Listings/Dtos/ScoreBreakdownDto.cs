namespace ImmoScorer.Application.Listings.Dtos;

/// <summary>DTO exposing the per-factor breakdown of a listing's opportunity score.</summary>
public sealed record ScoreBreakdownDto(
    int PriceGapScore,
    int AreaScore,
    int FloorScore,
    int EnergyScore,
    int TotalScore);
