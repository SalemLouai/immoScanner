using ImmoScorer.Domain.Enums;

namespace ImmoScorer.Domain.ValueObjects;

/// <summary>
/// DVF (Demande de Valeurs Foncières) reference price data for a postal code and property type.
/// Transient value object — not persisted as an entity.
/// </summary>
public sealed record DvfReferenceData(
    string PostalCode,
    PropertyType PropertyType,
    decimal MedianPricePerM2,
    int SampleCount,
    DateTime DataAsOf);
