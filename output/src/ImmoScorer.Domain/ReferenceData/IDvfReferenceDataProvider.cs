using ImmoScorer.Domain.Common;
using ImmoScorer.Domain.Enums;
using ImmoScorer.Domain.ValueObjects;

namespace ImmoScorer.Domain.ReferenceData;

/// <summary>
/// Provides DVF (Demande de Valeurs Foncières) reference price per m² for a given
/// postal code and property type.
/// </summary>
public interface IDvfReferenceDataProvider
{
    /// <summary>
    /// Retrieves the DVF reference data for <paramref name="postalCode"/> and
    /// <paramref name="propertyType"/>, fetching and caching from data.gouv.fr as needed.
    /// </summary>
    Task<Result<DvfReferenceData>> GetReferenceDataAsync(
        string postalCode,
        PropertyType propertyType,
        CancellationToken cancellationToken = default);
}
