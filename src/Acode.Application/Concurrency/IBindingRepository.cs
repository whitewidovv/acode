// src/Acode.Application/Concurrency/IBindingRepository.cs
namespace Acode.Application.Concurrency;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Concurrency;
using Acode.Domain.Conversation;
using Acode.Domain.Worktree;

/// <summary>
/// Repository interface for WorktreeBinding persistence.
/// Manages one-to-one mappings between worktrees and chats.
/// </summary>
public interface IBindingRepository
{
    /// <summary>
    /// Gets the binding for the specified worktree.
    /// </summary>
    /// <param name="worktreeId">The worktree identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The binding if found, otherwise null.</returns>
    Task<WorktreeBinding?> GetByWorktreeAsync(WorktreeId worktreeId, CancellationToken ct);

    /// <summary>
    /// Gets the binding for the specified chat.
    /// </summary>
    /// <param name="chatId">The chat identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The binding if found, otherwise null.</returns>
    Task<WorktreeBinding?> GetByChatAsync(ChatId chatId, CancellationToken ct);

    /// <summary>
    /// Creates a new binding.
    /// Enforces one-to-one constraint - fails if either worktree or chat is already bound.
    /// </summary>
    /// <param name="binding">The binding to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if binding already exists.</exception>
    Task CreateAsync(WorktreeBinding binding, CancellationToken ct);

    /// <summary>
    /// Deletes the binding for the specified worktree.
    /// </summary>
    /// <param name="worktreeId">The worktree identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(WorktreeId worktreeId, CancellationToken ct);

    /// <summary>
    /// Deletes the binding for the specified chat.
    /// </summary>
    /// <param name="chatId">The chat identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteByChatAsync(ChatId chatId, CancellationToken ct);

    /// <summary>
    /// Lists all active bindings.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all bindings, ordered by creation time (newest first).</returns>
    Task<IReadOnlyList<WorktreeBinding>> ListAllAsync(CancellationToken ct);
}
