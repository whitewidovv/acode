namespace Acode.Domain.Search;

/// <summary>
/// Specifies the sort order for search results.
/// </summary>
public enum SortOrder
{
    /// <summary>
    /// Sort by relevance score (highest first).
    /// </summary>
    Relevance,

    /// <summary>
    /// Sort by date descending (newest first).
    /// </summary>
    DateDescending,

    /// <summary>
    /// Sort by date ascending (oldest first).
    /// </summary>
    DateAscending
}
