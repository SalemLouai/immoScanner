using ImmoScorer.Domain.Entities;
using ImmoScorer.Domain.Scoring;
using ImmoScorer.Domain.ValueObjects;

namespace ImmoScorer.Infrastructure.Scoring;

/// <summary>
/// Implements a weighted scoring algorithm that combines four sub-scores to produce
/// an opportunity score in the range [0, 100].
/// </summary>
/// <remarks>
/// Score breakdown:
/// <list type="bullet">
///   <item>PriceGapScore (0–60): dominant factor — how far below the DVF median the listing sits.</item>
///   <item>AreaScore (0–15): bonus for larger area.</item>
///   <item>FloorScore (0–10): bonus for higher floors (better light, less noise).</item>
///   <item>EnergyScore (0–15): bonus for better DPE rating.</item>
/// </list>
/// </remarks>
public sealed class WeightedScoringEngine : IScoringEngine
{
    /// <inheritdoc/>
    public Score ComputeScore(Listing listing, DvfReferenceData referenceData)
    {
        var breakdown = ComputeBreakdown(listing, referenceData);
        var total = breakdown.PriceGapScore + breakdown.AreaScore
                    + breakdown.FloorScore + breakdown.EnergyScore;
        return new Score(total);
    }

    /// <inheritdoc/>
    public ScoreBreakdown ComputeBreakdown(Listing listing, DvfReferenceData referenceData)
    {
        var priceGap = ComputePriceGapScore(listing.PricePerM2, referenceData.MedianPricePerM2);
        var area = ComputeAreaScore(listing.Area);
        var floor = ComputeFloorScore(listing.Floor);
        var energy = ComputeEnergyScore(listing.EnergyRating);

        return new ScoreBreakdown(
            PriceGapScore: priceGap,
            AreaScore: area,
            FloorScore: floor,
            EnergyScore: energy);
    }

    /// <summary>
    /// Computes the price gap sub-score (0–60).
    /// The further the listing is below the DVF median, the higher the score.
    /// Above-median listings score 0. Equal to median scores 30.
    /// </summary>
    private static int ComputePriceGapScore(decimal listingPricePerM2, decimal medianPricePerM2)
    {
        if (medianPricePerM2 <= 0) return 30; // no reference data — neutral score

        var ratio = listingPricePerM2 / medianPricePerM2;

        // ratio < 0.7  => 60 (very underpriced)
        // ratio = 1.0  => 30 (at market)
        // ratio > 1.3  => 0  (overpriced)
        var rawScore = (1.3m - ratio) / 0.6m * 60m;
        return (int)Math.Clamp(rawScore, 0m, 60m);
    }

    /// <summary>
    /// Computes the area sub-score (0–15).
    /// Properties larger than 100 m² receive the full 15 points.
    /// </summary>
    private static int ComputeAreaScore(decimal areaSqM)
    {
        if (areaSqM <= 0) return 0;
        // Linear scale: 0 m² => 0, >= 100 m² => 15
        var raw = areaSqM / 100m * 15m;
        return (int)Math.Clamp(raw, 0m, 15m);
    }

    /// <summary>
    /// Computes the floor sub-score (0–10).
    /// Ground floor = 2 pts (natural light deficit), higher floors gain more points up to 10.
    /// </summary>
    private static int ComputeFloorScore(int? floor)
    {
        if (floor is null) return 5; // unknown — neutral

        return floor switch
        {
            0 => 2,
            1 => 4,
            2 => 6,
            3 => 7,
            4 => 8,
            5 => 9,
            _ => 10, // 6th floor and above
        };
    }

    /// <summary>
    /// Computes the energy sub-score (0–15) based on the DPE rating.
    /// </summary>
    private static int ComputeEnergyScore(string? rating)
    {
        return rating?.ToUpperInvariant() switch
        {
            "A" => 15,
            "B" => 13,
            "C" => 10,
            "D" => 7,
            "E" => 4,
            "F" => 2,
            "G" => 0,
            _ => 7, // unknown — neutral
        };
    }
}
