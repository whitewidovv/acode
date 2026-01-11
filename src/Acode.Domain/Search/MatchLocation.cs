namespace Acode.Domain.Search;

/// <summary>
/// Represents the location of a matching term within a field.
/// </summary>
public sealed record MatchLocation
{
    /// <summary>
    /// Gets the field name where the match occurred (e.g., "content", "title", "tags").
    /// </summary>
    public required string Field { get; init; }

    /// <summary>
    /// Gets the character offset where the match starts.
    /// </summary>
    public required int StartOffset { get; init; }

    /// <summary>
    /// Gets the length of the matching text in characters.
    /// </summary>
    public required int Length { get; init; }
}
