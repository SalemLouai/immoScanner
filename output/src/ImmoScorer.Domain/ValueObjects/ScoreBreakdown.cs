namespace ImmoScorer.Domain.ValueObjects;

/// <summary>
/// Breakdown of how the overall opportunity score was computed.
/// </summary>
/// <param name="PriceGapScore">Score contribution from the price gap vs DVF median (0–60).</param>
/// <param name="AreaScore">Score contribution from the property area relative to the search criteria (0–15).</param>
/// <param name="FloorScore">Score contribution from the floor level (0–10).</param>
/// <param name="EnergyScore">Score contribution from the DPE energy rating (0–15).</param>
public sealed record ScoreBreakdown(
    int PriceGapScore,
    int AreaScore,
    int FloorScore,
    int EnergyScore);
