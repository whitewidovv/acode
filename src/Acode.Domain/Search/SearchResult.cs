using Acode.Domain.Conversation;
using Acode.Domain.Models.Inference;

namespace Acode.Domain.Search;

/// <summary>
/// Represents a single search result matching the query.
/// </summary>
public sealed record SearchResult
{
    /// <summary>
    /// Gets the ID of the matched message.
    /// </summary>
    public required MessageId MessageId { get; init; }

    /// <summary>
    /// Gets the ID of the chat containing the message.
    /// </summary>
    public required ChatId ChatId { get; init; }

    /// <summary>
    /// Gets the title of the chat containing the message.
    /// </summary>
    public required string ChatTitle { get; init; }

    /// <summary>
    /// Gets the role of the message (user, assistant, system, tool).
    /// </summary>
    public required MessageRole Role { get; init; }

    /// <summary>
    /// Gets the timestamp when the message was created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the snippet of the message content with highlighted matching terms.
    /// </summary>
    public required string Snippet { get; init; }

    /// <summary>
    /// Gets the relevance score for this result (higher = more relevant).
    /// </summary>
    public required double Score { get; init; }

    /// <summary>
    /// Gets the locations of matching terms within the content.
    /// </summary>
    public IReadOnlyList<MatchLocation> Matches { get; init; } = Array.Empty<MatchLocation>();
}
