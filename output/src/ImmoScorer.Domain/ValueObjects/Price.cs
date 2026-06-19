namespace ImmoScorer.Domain.ValueObjects;

/// <summary>Monetary price of a listing.</summary>
public sealed record Price(decimal Amount, string Currency = "EUR");
