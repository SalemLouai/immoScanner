using ImmoScorer.Application.Listings.Dtos;
using ImmoScorer.Domain.Common;
using ImmoScorer.Domain.ValueObjects;
using MediatR;

namespace ImmoScorer.Application.Listings.Queries;

/// <summary>Query to retrieve the full detail of a single listing by its ID.</summary>
public sealed record GetListingDetailQuery(ListingId ListingId)
    : IRequest<Result<ListingDetailDto>>;
