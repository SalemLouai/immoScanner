using ImmoScorer.Domain.Common;
using ImmoScorer.Domain.ValueObjects;
using MediatR;

namespace ImmoScorer.Application.Searches.Commands;

/// <summary>
/// Command to create a new real-estate search from the given criteria.
/// On success, returns the newly created <see cref="SearchId"/>.
/// </summary>
public sealed record RunSearchCommand(SearchCriteria Criteria)
    : IRequest<Result<SearchId>>;
