namespace Acode.Domain.Search;

/// <summary>
/// Represents a collection of search results with pagination metadata.
/// </summary>
public sealed record SearchResults
{
    /// <summary>
    /// Gets the list of search results for the current page.
    /// </summary>
    public required IReadOnlyList<SearchResult> Results { get; init; }

    /// <summary>
    /// Gets the total number of results matching the query across all pages.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Gets the current page number (1-based).
    /// </summary>
    public required int PageNumber { get; init; }

    /// <summary>
    /// Gets the number of results per page.
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// Gets the query execution time in milliseconds.
    /// </summary>
    public required double QueryTimeMs { get; init; }

    /// <summary>
    /// Gets the optional cursor for fetching the next page of results.
    /// </summary>
    public string? NextCursor { get; init; }

    /// <summary>
    /// Gets the total number of pages available.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Gets a value indicating whether there is a next page of results.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Gets a value indicating whether there is a previous page of results.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
}
