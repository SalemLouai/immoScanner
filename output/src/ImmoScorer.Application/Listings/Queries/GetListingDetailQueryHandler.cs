using ImmoScorer.Application.Common.Persistence;
using ImmoScorer.Application.Listings.Dtos;
using ImmoScorer.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ImmoScorer.Application.Listings.Queries;

/// <summary>
/// Handles <see cref="GetListingDetailQuery"/>: fetches a single listing and projects it to
/// <see cref="ListingDetailDto"/>.
/// </summary>
public sealed class GetListingDetailQueryHandler(IImmoScorerDbContext db)
    : IRequestHandler<GetListingDetailQuery, Result<ListingDetailDto>>
{
    /// <inheritdoc/>
    public async Task<Result<ListingDetailDto>> Handle(
        GetListingDetailQuery request,
        CancellationToken cancellationToken)
    {
        var listing = await db.Listings
            .AsNoTracking()
            .Where(l => l.Id == request.ListingId)
            .Select(l => new ListingDetailDto(
                l.Id.Value,
                l.Title,
                l.Description,
                l.Source,
                l.Address.City,
                l.Address.PostalCode,
                l.Price.Amount,
                l.Area,
                l.Rooms,
                l.Floor,
                l.EnergyRating,
                l.PricePerM2,
                l.ReferencePricePerM2,
                l.Score.Value,
                new ScoreBreakdownDto(
                    l.ScoreBreakdown.PriceGapScore,
                    l.ScoreBreakdown.AreaScore,
                    l.ScoreBreakdown.FloorScore,
                    l.ScoreBreakdown.EnergyScore,
                    l.Score.Value),
                l.OriginalUrl,
                l.ScrapedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (listing is null)
        {
            return Result<ListingDetailDto>.Failure(
                $"Listing {request.ListingId} not found.");
        }

        return Result<ListingDetailDto>.Success(listing);
    }
}
