namespace ImmoScorer.Domain.ValueObjects;

/// <summary>Strongly-typed identifier for a <see cref="Entities.Search"/>.</summary>
public readonly record struct SearchId(Guid Value)
{
    /// <summary>Creates a new <see cref="SearchId"/> with a freshly generated GUID.</summary>
    public static SearchId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
