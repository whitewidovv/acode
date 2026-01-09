// src/Acode.Application/Conversation/Persistence/IRunRepository.cs
namespace Acode.Application.Conversation.Persistence;

using Acode.Domain.Conversation;

/// <summary>
/// Repository interface for Run entity persistence.
/// </summary>
public interface IRunRepository
{
    /// <summary>Creates a new Run and returns its ID.</summary>
    /// <param name="run">The Run entity to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The ID of the created Run.</returns>
    Task<RunId> CreateAsync(Run run, CancellationToken ct);

    /// <summary>Gets a Run by ID.</summary>
    /// <param name="id">The Run ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The Run entity, or null if not found.</returns>
    Task<Run?> GetByIdAsync(RunId id, CancellationToken ct);

    /// <summary>Updates an existing Run.</summary>
    /// <param name="run">The Run entity to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UpdateAsync(Run run, CancellationToken ct);

    /// <summary>Lists all Runs for a specific Chat, ordered by sequence number.</summary>
    /// <param name="chatId">The Chat ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A readonly list of Run entities.</returns>
    Task<IReadOnlyList<Run>> ListByChatAsync(ChatId chatId, CancellationToken ct);

    /// <summary>Gets the latest Run for a specific Chat.</summary>
    /// <param name="chatId">The Chat ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The latest Run entity, or null if no runs exist.</returns>
    Task<Run?> GetLatestAsync(ChatId chatId, CancellationToken ct);

    /// <summary>Permanently deletes a Run (for cascade purge operations).</summary>
    /// <param name="id">The Run ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    Task DeleteAsync(RunId id, CancellationToken ct);
}
