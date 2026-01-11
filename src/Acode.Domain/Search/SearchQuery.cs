using Acode.Domain.Conversation;
using Acode.Domain.Database;
using Acode.Domain.Models.Inference;

namespace Acode.Domain.Search;

/// <summary>
/// Represents a search query with filters and pagination.
/// </summary>
public sealed record SearchQuery
{
    /// <summary>
    /// Gets the search query text.
    /// </summary>
    public required string QueryText { get; init; }

    /// <summary>
    /// Gets the optional chat ID filter.
    /// </summary>
    public ChatId? ChatId { get; init; }

    /// <summary>
    /// Gets the optional start date filter (messages after this date).
    /// </summary>
    public DateTime? Since { get; init; }

    /// <summary>
    /// Gets the optional end date filter (messages before this date).
    /// </summary>
    public DateTime? Until { get; init; }

    /// <summary>
    /// Gets the optional message role filter.
    /// </summary>
    public MessageRole? RoleFilter { get; init; }

    /// <summary>
    /// Gets the page size for pagination (1-100, default 20).
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Gets the page number for pagination (default 1).
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Gets the sort order for results.
    /// </summary>
    public SortOrder SortBy { get; init; } = SortOrder.Relevance;

    /// <summary>
    /// Validates the search query.
    /// </summary>
    /// <returns>A validation result indicating success or failure.</returns>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(QueryText))
        {
            errors.Add("Query text cannot be empty");
        }

        if (QueryText?.Length > 200)
        {
            errors.Add("Query text must be ≤200 characters");
        }

        if (PageSize < 1 || PageSize > 100)
        {
            errors.Add("Page size must be between 1 and 100");
        }

        // Date validation (P4.3 - AC-123)
        if (Since.HasValue && Since.Value > DateTime.UtcNow)
        {
            errors.Add("Since date cannot be in the future");
        }

        if (Until.HasValue && Until.Value > DateTime.UtcNow)
        {
            errors.Add("Until date cannot be in the future");
        }

        if (Since.HasValue && Until.HasValue && Since.Value > Until.Value)
        {
            errors.Add("Since date must be before Until date");
        }

        return errors.Count > 0
            ? ValidationResult.Failure(errors.ToArray())
            : ValidationResult.Success();
    }
}
