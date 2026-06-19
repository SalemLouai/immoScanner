using ImmoScorer.Application.Listings.Dtos;
using ImmoScorer.Application.Listings.Queries;
using ImmoScorer.Domain.Enums;
using ImmoScorer.Domain.ValueObjects;
using MediatR;

namespace ImmoScorer.Api.Endpoints;

/// <summary>Extension method that maps all listing-related Minimal API endpoints.</summary>
public static class ListingEndpoints
{
    /// <summary>Maps the listing endpoints onto the given <see cref="WebApplication"/>.</summary>
    public static void MapListingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/listings")
            .WithTags("Listings")
            .WithOpenApi();

        // GET /listings?searchId=&minScore=&maxPrice=&minArea=&city=&source=&sort=&page=&pageSize=
        group.MapGet("/", async (
            Guid searchId,
            IMediator mediator,
            CancellationToken ct,
            int? minScore = null,
            decimal? maxPrice = null,
            decimal? minArea = null,
            string? city = null,
            string? source = null,
            string? sort = null,
            int page = 1,
            int pageSize = 20) =>
        {
            var sortOrder = sort?.ToLowerInvariant() switch
            {
                "price_asc" => ListingSortOrder.PriceAscending,
                "price_desc" => ListingSortOrder.PriceDescending,
                "price_per_m2_asc" => ListingSortOrder.PricePerM2Ascending,
                "date_desc" => ListingSortOrder.DateDescending,
                _ => ListingSortOrder.ScoreDescending,
            };

            var filter = (minScore.HasValue || maxPrice.HasValue || minArea.HasValue
                          || city is not null || source is not null)
                ? new ListingFilter(minScore, maxPrice, minArea, city, source)
                : null;

            var query = new GetListingsQuery(
                new SearchId(searchId),
                filter,
                sortOrder,
                page,
                pageSize);

            var result = await mediator.Send(query, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("GetListings")
        .WithSummary("Retrieve a paginated, filtered and sorted list of listings for a search.");

        // GET /listings/{id}
        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetListingDetailQuery(new ListingId(id)), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("GetListingDetail")
        .WithSummary("Retrieve the full detail of a single listing including score breakdown.");
    }
}
