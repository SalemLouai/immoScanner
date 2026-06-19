namespace ImmoScorer.Domain.ValueObjects;

/// <summary>Strongly-typed identifier for a <see cref="Entities.Listing"/>.</summary>
public readonly record struct ListingId(Guid Value)
{
    /// <summary>Creates a new <see cref="ListingId"/> with a freshly generated GUID.</summary>
    public static ListingId New() => new(Guid.NewGuid());

    /// <inheritdoc/>
    public override string ToString() => Value.ToString();
}
