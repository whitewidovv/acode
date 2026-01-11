// src/Acode.Application/Conversation/Persistence/IMessageRepository.cs
namespace Acode.Application.Conversation.Persistence;

using Acode.Domain.Conversation;

/// <summary>
/// Repository interface for Message entity persistence.
/// </summary>
public interface IMessageRepository
{
    /// <summary>Creates a new Message and returns its ID.</summary>
    /// <param name="message">The Message entity to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The ID of the created Message.</returns>
    Task<MessageId> CreateAsync(Message message, CancellationToken ct);

    /// <summary>Gets a Message by ID.</summary>
    /// <param name="id">The Message ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Message entity, or null if not found.</returns>
    Task<Message?> GetByIdAsync(MessageId id, CancellationToken ct);

    /// <summary>Updates an existing Message.</summary>
    /// <param name="message">The Message entity to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UpdateAsync(Message message, CancellationToken ct);

    /// <summary>Lists all Messages for a specific Run, ordered by sequence number.</summary>
    /// <param name="runId">The Run ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A readonly list of Message entities.</returns>
    Task<IReadOnlyList<Message>> ListByRunAsync(RunId runId, CancellationToken ct);

    /// <summary>Permanently deletes all Messages for a specific Run (for cascade purge operations).</summary>
    /// <param name="runId">The Run ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task DeleteByRunAsync(RunId runId, CancellationToken ct);

    /// <summary>Appends a Message to a specific Run with auto-assigned sequence number.</summary>
    /// <param name="runId">The Run ID to append the message to.</param>
    /// <param name="message">The Message entity to append.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The ID of the created Message.</returns>
    Task<MessageId> AppendAsync(RunId runId, Message message, CancellationToken ct);

    /// <summary>Bulk inserts multiple Messages efficiently in a single operation.</summary>
    /// <param name="messages">The collection of Message entities to insert.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task BulkCreateAsync(IEnumerable<Message> messages, CancellationToken ct);
}
