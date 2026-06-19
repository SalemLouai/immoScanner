using ImmoScorer.Application.Common;
using ImmoScorer.Application.Common.Persistence;
using ImmoScorer.Application.Listings.Dtos;
using ImmoScorer.Domain.Common;
using ImmoScorer.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ImmoScorer.Application.Listings.Queries;

/// <summary>
/// Handles <see cref="GetListingsQuery"/>: applies filters, sort order and pagination
/// against the listings table, then projects to <see cref="ListingDto"/>.
/// </summary>
public sealed class GetListingsQueryHandler(IImmoScorerDbContext db)
    : IRequestHandler<GetListingsQuery, Result<PaginatedList<ListingDto>>>
{
    /// <inheritdoc/>
    public async Task<Result<PaginatedList<ListingDto>>> Handle(
        GetListingsQuery request,
        CancellationToken cancellationToken)
    {
        var query = db.Listings
            .AsNoTracking()
            .Where(l => l.SearchId == request.SearchId);

        // Apply filters
        var filter = request.Filter;
        if (filter is not null)
        {
            if (filter.MinScore.HasValue)
                query = query.Where(l => l.Score.Value >= filter.MinScore.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(l => l.Price.Amount <= filter.MaxPrice.Value);

            if (filter.MinArea.HasValue)
                query = query.Where(l => l.Area >= filter.MinArea.Value);

            if (!string.IsNullOrWhiteSpace(filter.City))
                query = query.Where(l => l.Address.City == filter.City);

            if (!string.IsNullOrWhiteSpace(filter.Source))
                query = query.Where(l => l.Source == filter.Source);
        }

        // Apply sort order
        query = request.SortOrder switch
        {
            ListingSortOrder.ScoreDescending => query.OrderByDescending(l => l.Score.Value),
            ListingSortOrder.PriceAscending => query.OrderBy(l => l.Price.Amount),
            ListingSortOrder.PriceDescending => query.OrderByDescending(l => l.Price.Amount),
            ListingSortOrder.PricePerM2Ascending => query.OrderBy(l => l.PricePerM2),
            ListingSortOrder.DateDescending => query.OrderByDescending(l => l.ScrapedAt),
            _ => query.OrderByDescending(l => l.Score.Value),
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new ListingDto(
                l.Id.Value,
                l.Title,
                l.Source,
                l.Address.City,
                l.Address.PostalCode,
                l.Price.Amount,
                l.Area,
                l.PricePerM2,
                l.ReferencePricePerM2,
                l.Score.Value,
                l.OriginalUrl,
                l.ScrapedAt))
            .ToListAsync(cancellationToken);

        return Result<PaginatedList<ListingDto>>.Success(
            new PaginatedList<ListingDto>(items, totalCount, page, pageSize));
    }
}
