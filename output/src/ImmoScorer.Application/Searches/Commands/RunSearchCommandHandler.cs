using ImmoScorer.Application.Common.Persistence;
using ImmoScorer.Domain.Common;
using ImmoScorer.Domain.Entities;
using ImmoScorer.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ImmoScorer.Application.Searches.Commands;

/// <summary>
/// Handles <see cref="RunSearchCommand"/>: persists the new search and returns its ID.
/// Scraping is triggered separately via <see cref="Scraping.Commands.TriggerScrapingCommand"/>.
/// </summary>
public sealed class RunSearchCommandHandler(
    IImmoScorerDbContext db,
    ILogger<RunSearchCommandHandler> logger)
    : IRequestHandler<RunSearchCommand, Result<SearchId>>
{
    /// <inheritdoc/>
    public async Task<Result<SearchId>> Handle(
        RunSearchCommand request,
        CancellationToken cancellationToken)
    {
        var search = Search.Create(request.Criteria);
        db.Searches.Add(search);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Search {SearchId} created for city {City}",
            search.Id,
            request.Criteria.City);

        return Result<SearchId>.Success(search.Id);
    }
}
