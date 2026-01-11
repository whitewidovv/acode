using Acode.Domain.Conversation;
using Acode.Domain.Search;

namespace Acode.Application.Interfaces;

/// <summary>
/// Service for full-text search over conversation history.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Searches conversation history based on the given query.
    /// </summary>
    /// <param name="query">The search query with filters and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results matching the query.</returns>
    Task<SearchResults> SearchAsync(SearchQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Indexes a message for searching.
    /// </summary>
    /// <param name="message">The message to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task IndexMessageAsync(Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the search index for an existing message.
    /// </summary>
    /// <param name="message">The message with updated content.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task UpdateMessageIndexAsync(Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Removes a message from the search index.
    /// </summary>
    /// <param name="messageId">The ID of the message to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task RemoveFromIndexAsync(MessageId messageId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the current status of the search index.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The index status information.</returns>
    Task<IndexStatus> GetIndexStatusAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Rebuilds the entire search index from scratch.
    /// </summary>
    /// <param name="progress">Optional progress reporter for tracking rebuild progress.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task RebuildIndexAsync(IProgress<int>? progress, CancellationToken cancellationToken);

    /// <summary>
    /// Rebuilds the search index for a specific chat.
    /// </summary>
    /// <param name="chatId">The chat ID to rebuild the index for.</param>
    /// <param name="progress">Optional progress reporter for tracking rebuild progress.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task RebuildIndexAsync(ChatId chatId, IProgress<int>? progress, CancellationToken cancellationToken);

    /// <summary>
    /// Optimizes the search index by merging FTS5 segments.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task OptimizeIndexAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Represents the status of the search index.
/// </summary>
public sealed record IndexStatus
{
    /// <summary>
    /// Gets the number of messages currently indexed.
    /// </summary>
    public required int IndexedMessageCount { get; init; }

    /// <summary>
    /// Gets the total number of messages in the database.
    /// </summary>
    public required int TotalMessageCount { get; init; }

    /// <summary>
    /// Gets a value indicating whether the index is healthy (fully synced).
    /// </summary>
    public required bool IsHealthy { get; init; }

    /// <summary>
    /// Gets the optional timestamp of the last index optimization.
    /// </summary>
    public DateTime? LastOptimizedAt { get; init; }

    /// <summary>
    /// Gets the size of the search index in bytes.
    /// </summary>
    public long IndexSizeBytes { get; init; }

    /// <summary>
    /// Gets the number of FTS5 index segments (lower is better for performance).
    /// </summary>
    public int SegmentCount { get; init; }

    /// <summary>
    /// Gets the number of pending messages awaiting indexing.
    /// </summary>
    public int PendingCount => TotalMessageCount - IndexedMessageCount;
}
