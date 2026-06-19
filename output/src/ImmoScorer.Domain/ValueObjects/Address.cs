namespace ImmoScorer.Domain.ValueObjects;

/// <summary>Geographic address of a listing.</summary>
public sealed record Address(
    string City,
    string PostalCode,
    string? Neighborhood);
