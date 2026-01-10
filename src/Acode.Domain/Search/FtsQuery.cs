using Acode.Domain.Models.Inference;

namespace Acode.Domain.Search;

/// <summary>
/// Represents a parsed full-text search query with boolean operators and field filters.
/// </summary>
public sealed class FtsQuery
{
    /// <summary>
    /// Gets the query text converted to FTS5 syntax.
    /// </summary>
    public required string Fts5Syntax { get; init; }

    /// <summary>
    /// Gets the count of boolean operators (AND, OR, NOT) in the query.
    /// </summary>
    public required int OperatorCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether the query is syntactically valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Gets the error message if the query is invalid, or null if valid.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the role filter extracted from role: prefix, or null if not specified.
    /// </summary>
    public MessageRole? RoleFilter { get; init; }

    /// <summary>
    /// Gets the chat ID filter extracted from chat: prefix (as GUID), or null if not specified.
    /// </summary>
    public Guid? ChatIdFilter { get; init; }

    /// <summary>
    /// Gets the chat name filter extracted from chat: prefix (as string), or null if not specified.
    /// </summary>
    public string? ChatNameFilter { get; init; }

    /// <summary>
    /// Gets the tag filter extracted from tag: prefix, or null if not specified.
    /// </summary>
    public string? TagFilter { get; init; }

    /// <summary>
    /// Gets the list of title terms extracted from title: prefixes.
    /// </summary>
    public List<string> TitleTerms { get; init; } = new();
}
