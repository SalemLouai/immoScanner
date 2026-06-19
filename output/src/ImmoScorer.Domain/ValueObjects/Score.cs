namespace ImmoScorer.Domain.ValueObjects;

/// <summary>Opportunity score for a listing, clamped to the range [0, 100].</summary>
public sealed record Score
{
    /// <summary>Gets the clamped score value between 0 and 100.</summary>
    public int Value { get; }

    /// <summary>
    /// Initialises a new <see cref="Score"/>, clamping <paramref name="value"/> to [0, 100].
    /// </summary>
    public Score(int value) => Value = Math.Clamp(value, 0, 100);
}
