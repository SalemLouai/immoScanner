using ImmoScorer.Domain.Common;
using ImmoScorer.Domain.ValueObjects;
using MediatR;

namespace ImmoScorer.Application.Scraping.Commands;

/// <summary>
/// Command to trigger scraping for an existing search by enqueuing one job per registered source.
/// </summary>
public sealed record TriggerScrapingCommand(SearchId SearchId)
    : IRequest<Result<Unit>>;
