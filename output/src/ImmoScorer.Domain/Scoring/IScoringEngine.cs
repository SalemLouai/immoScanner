using ImmoScorer.Domain.Entities;
using ImmoScorer.Domain.ValueObjects;

namespace ImmoScorer.Domain.Scoring;

/// <summary>
/// Computes an opportunity score (0–100) for a listing given DVF reference price data.
/// </summary>
public interface IScoringEngine
{
    /// <summary>
    /// Computes the score and its breakdown for <paramref name="listing"/>
    /// using <paramref name="referenceData"/> as the DVF benchmark.
    /// </summary>
    Score ComputeScore(Listing listing, DvfReferenceData referenceData);

    /// <summary>
    /// Computes the full score breakdown for <paramref name="listing"/>
    /// using <paramref name="referenceData"/> as the DVF benchmark.
    /// </summary>
    ScoreBreakdown ComputeBreakdown(Listing listing, DvfReferenceData referenceData);
}
