namespace Acode.Domain.Search;

/// <summary>
/// Represents a parsed full-text search query with boolean operators.
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
}
