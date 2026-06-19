using ImmoScorer.Application.Scraping.Commands;
using ImmoScorer.Application.Searches.Commands;
using ImmoScorer.Application.Searches.Queries;
using ImmoScorer.Domain.ValueObjects;
using MediatR;

namespace ImmoScorer.Api.Endpoints;

/// <summary>Extension method that maps all search-related Minimal API endpoints.</summary>
public static class SearchEndpoints
{
    /// <summary>Maps the search endpoints onto the given <see cref="WebApplication"/>.</summary>
    public static void MapSearchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/searches")
            .WithTags("Searches")
            .WithOpenApi();

        // POST /searches — create a new search
        group.MapPost("/", async (RunSearchCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/searches/{result.Value.Value}", new { searchId = result.Value.Value })
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("CreateSearch")
        .WithSummary("Create a new real-estate search from the provided criteria.");

        // GET /searches — list all saved searches
        group.MapGet("/", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetSavedSearchesQuery(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(result.Error);
        })
        .WithName("ListSearches")
        .WithSummary("Retrieve all saved searches.");

        // POST /searches/{id}/scrape — trigger scraping for an existing search
        group.MapPost("/{id:guid}/scrape", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new TriggerScrapingCommand(new SearchId(id)), ct);
            return result.IsSuccess
                ? Results.Accepted($"/searches/{id}", new { message = "Scraping triggered." })
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("TriggerScraping")
        .WithSummary("Trigger scraping for an existing search.");
    }
}
