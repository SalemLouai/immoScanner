using ImmoScorer.Application.Common.Persistence;
using ImmoScorer.Application.Searches.Dtos;
using ImmoScorer.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ImmoScorer.Application.Searches.Queries;

/// <summary>
/// Handles <see cref="GetSavedSearchesQuery"/>: returns all searches
/// with their listing counts, ordered by creation date descending.
/// </summary>
public sealed class GetSavedSearchesQueryHandler(IImmoScorerDbContext db)
    : IRequestHandler<GetSavedSearchesQuery, Result<IReadOnlyList<SavedSearchDto>>>
{
    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<SavedSearchDto>>> Handle(
        GetSavedSearchesQuery request,
        CancellationToken cancellationToken)
    {
        var results = await db.Searches
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SavedSearchDto(
                s.Id.Value,
                s.Criteria.City,
                s.Criteria.PostalCode,
                s.Criteria.PropertyType,
                s.Status.ToString(),
                db.Listings.Count(l => l.SearchId == s.Id),
                s.CreatedAt,
                s.CompletedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<SavedSearchDto>>.Success(results);
    }
}
