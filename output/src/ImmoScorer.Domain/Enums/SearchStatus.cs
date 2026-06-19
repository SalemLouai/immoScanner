namespace ImmoScorer.Domain.Enums;

/// <summary>Lifecycle status of a search.</summary>
public enum SearchStatus
{
    /// <summary>Search has been created but scraping has not started.</summary>
    Created,

    /// <summary>Scraping is currently in progress.</summary>
    InProgress,

    /// <summary>All scraping jobs have completed successfully.</summary>
    Completed,

    /// <summary>Scraping failed for this search.</summary>
    Failed,
}
