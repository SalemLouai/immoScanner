using ImmoScorer.Domain.Enums;
using ImmoScorer.Domain.ValueObjects;

namespace ImmoScorer.Domain.Entities;

/// <summary>
/// Aggregate root representing a saved real-estate search and its associated listings.
/// </summary>
public sealed class Search
{
    private readonly List<Listing> _listings = [];

    /// <summary>Initialises a new <see cref="Search"/> (EF Core constructor).</summary>
    private Search() { }

    /// <summary>Creates a new search with a freshly-generated ID.</summary>
    public static Search Create(SearchCriteria criteria)
    {
        return new Search
        {
            Id = SearchId.New(),
            Criteria = criteria,
            Status = SearchStatus.Created,
            CreatedAt = DateTime.UtcNow,
        };
    }

    /// <summary>Gets the unique identifier for this search.</summary>
    public SearchId Id { get; private set; }

    /// <summary>Gets the criteria used to run this search.</summary>
    public SearchCriteria Criteria { get; private set; } = null!;

    /// <summary>Gets the current processing status of this search.</summary>
    public SearchStatus Status { get; private set; }

    /// <summary>Gets the UTC timestamp when this search was created.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Gets the UTC timestamp when this search completed, or <c>null</c> if still in progress.</summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>Gets the listings associated with this search.</summary>
    public IReadOnlyCollection<Listing> Listings => _listings.AsReadOnly();

    /// <summary>Marks the search as in-progress.</summary>
    public void MarkInProgress() => Status = SearchStatus.InProgress;

    /// <summary>Marks the search as completed and records the completion timestamp.</summary>
    public void MarkCompleted()
    {
        Status = SearchStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>Marks the search as failed and records the completion timestamp.</summary>
    public void MarkFailed()
    {
        Status = SearchStatus.Failed;
        CompletedAt = DateTime.UtcNow;
    }
}
