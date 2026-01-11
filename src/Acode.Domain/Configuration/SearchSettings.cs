namespace Acode.Domain.Configuration;

/// <summary>
/// Search configuration settings for full-text search behavior.
/// </summary>
/// <remarks>
/// Covers AC-054, AC-055, AC-059, AC-065 for configurable search parameters.
/// </remarks>
public sealed record SearchSettings
{
    /// <summary>
    /// Gets a value indicating whether recency boost is enabled.
    /// </summary>
    /// <remarks>
    /// AC-055: Recency boost can be disabled via configuration.
    /// </remarks>
    public bool RecencyBoostEnabled { get; init; } = true;

    /// <summary>
    /// Gets the recency boost multiplier for messages within 24 hours.
    /// </summary>
    /// <remarks>
    /// AC-054: Recency boost multiplier for recent messages.
    /// Default: 1.5x boost for messages within 24 hours.
    /// </remarks>
    public double RecencyBoost24Hours { get; init; } = 1.5;

    /// <summary>
    /// Gets the recency boost multiplier for messages within 7 days.
    /// </summary>
    /// <remarks>
    /// AC-054: Recency boost multiplier for recent messages.
    /// Default: 1.2x boost for messages within 7 days.
    /// </remarks>
    public double RecencyBoost7Days { get; init; } = 1.2;

    /// <summary>
    /// Gets the default recency boost multiplier for older messages.
    /// </summary>
    /// <remarks>
    /// AC-054: Recency boost multiplier for older messages.
    /// Default: 1.0x (no boost) for messages older than 7 days.
    /// </remarks>
    public double RecencyBoostDefault { get; init; } = 1.0;

    /// <summary>
    /// Gets the maximum snippet length in characters.
    /// </summary>
    /// <remarks>
    /// AC-059: Snippet length is configurable (default 150, range 50-500).
    /// </remarks>
    public int SnippetMaxLength { get; init; } = 150;

    /// <summary>
    /// Gets the minimum snippet length in characters.
    /// </summary>
    /// <remarks>
    /// Lower bound validation for snippet length.
    /// </remarks>
    public int SnippetMinLength { get; init; } = 50;

    /// <summary>
    /// Gets the maximum allowed snippet length limit in characters.
    /// </summary>
    /// <remarks>
    /// Upper bound validation for snippet length.
    /// </remarks>
    public int SnippetMaxLengthLimit { get; init; } = 500;

    /// <summary>
    /// Gets the opening tag for highlighting matched terms.
    /// </summary>
    /// <remarks>
    /// AC-065: Highlight tags are configurable (default &lt;mark&gt;).
    /// Can be set to ANSI codes, HTML tags, or custom markers.
    /// </remarks>
    public string HighlightOpenTag { get; init; } = "<mark>";

    /// <summary>
    /// Gets the closing tag for highlighting matched terms.
    /// </summary>
    /// <remarks>
    /// AC-065: Highlight tags are configurable (default &lt;/mark&gt;).
    /// Can be set to ANSI codes, HTML tags, or custom markers.
    /// </remarks>
    public string HighlightCloseTag { get; init; } = "</mark>";

    /// <summary>
    /// Gets the default query timeout.
    /// </summary>
    /// <remarks>
    /// Default timeout for search queries (5 seconds).
    /// </remarks>
    public TimeSpan DefaultQueryTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the default page size for search results.
    /// </summary>
    /// <remarks>
    /// Default: 20 results per page.
    /// </remarks>
    public int DefaultPageSize { get; init; } = 20;

    /// <summary>
    /// Gets the maximum allowed page size for search results.
    /// </summary>
    /// <remarks>
    /// Upper bound validation for pagination.
    /// </remarks>
    public int MaxPageSize { get; init; } = 100;

    /// <summary>
    /// Gets a value indicating whether messages should be automatically indexed.
    /// </summary>
    /// <remarks>
    /// When true, messages are indexed automatically via database triggers.
    /// When false, manual indexing is required.
    /// </remarks>
    public bool AutoIndexMessages { get; init; } = true;

    /// <summary>
    /// Gets the delay before updating the index after message changes.
    /// </summary>
    /// <remarks>
    /// Debounce interval for index updates (default 100ms).
    /// </remarks>
    public TimeSpan IndexUpdateDelay { get; init; } = TimeSpan.FromMilliseconds(100);
}
