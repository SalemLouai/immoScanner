using ImmoScorer.Application.Common;
using ImmoScorer.Application.Listings.Dtos;
using ImmoScorer.Domain.Common;
using ImmoScorer.Domain.Enums;
using ImmoScorer.Domain.ValueObjects;
using MediatR;

namespace ImmoScorer.Application.Listings.Queries;

/// <summary>
/// Query to retrieve a paginated, filtered, and sorted list of listings for a given search.
/// </summary>
public sealed record GetListingsQuery(
    SearchId SearchId,
    ListingFilter? Filter,
    ListingSortOrder SortOrder,
    int Page,
    int PageSize)
    : IRequest<Result<PaginatedList<ListingDto>>>;
