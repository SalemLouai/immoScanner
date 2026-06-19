using ImmoScorer.Application.Searches.Dtos;
using ImmoScorer.Domain.Common;
using MediatR;

namespace ImmoScorer.Application.Searches.Queries;

/// <summary>Query to retrieve all saved searches, ordered by creation date descending.</summary>
public sealed record GetSavedSearchesQuery()
    : IRequest<Result<IReadOnlyList<SavedSearchDto>>>;
