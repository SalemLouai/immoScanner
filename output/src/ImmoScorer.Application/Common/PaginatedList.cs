namespace ImmoScorer.Application.Common;

/// <summary>
/// A wrapper around a paged subset of items, carrying pagination metadata.
/// </summary>
/// <typeparam name="T">The type of items in the page.</typeparam>
public sealed record PaginatedList<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    /// <summary>Gets the total number of pages.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    /// <summary>Gets a value indicating whether there is a next page.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Gets a value indicating whether there is a previous page.</summary>
    public bool HasPreviousPage => Page > 1;
}
