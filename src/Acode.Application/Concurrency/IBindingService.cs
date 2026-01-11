// src/Acode.Application/Concurrency/IBindingService.cs
namespace Acode.Application.Concurrency;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Concurrency;
using Acode.Domain.Conversation;
using Acode.Domain.Worktree;

/// <summary>
/// Application service for managing worktree-to-chat bindings.
/// Enforces one-to-one relationship between worktrees and chats.
/// </summary>
public interface IBindingService
{
    /// <summary>
    /// Gets the chat bound to the specified worktree.
    /// </summary>
    /// <param name="worktreeId">The worktree identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The bound chat ID, or null if worktree is not bound.</returns>
    Task<ChatId?> GetBoundChatAsync(WorktreeId worktreeId, CancellationToken ct);

    /// <summary>
    /// Gets the worktree bound to the specified chat.
    /// </summary>
    /// <param name="chatId">The chat identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The bound worktree ID, or null if chat is not bound.</returns>
    Task<WorktreeId?> GetBoundWorktreeAsync(ChatId chatId, CancellationToken ct);

    /// <summary>
    /// Creates a new binding between a worktree and a chat.
    /// Enforces one-to-one constraint - fails if either is already bound.
    /// </summary>
    /// <param name="worktreeId">The worktree identifier.</param>
    /// <param name="chatId">The chat identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if binding already exists.</exception>
    Task CreateBindingAsync(WorktreeId worktreeId, ChatId chatId, CancellationToken ct);

    /// <summary>
    /// Deletes the binding for the specified worktree.
    /// </summary>
    /// <param name="worktreeId">The worktree identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task DeleteBindingAsync(WorktreeId worktreeId, CancellationToken ct);

    /// <summary>
    /// Lists all active worktree bindings.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all bindings.</returns>
    Task<IReadOnlyList<WorktreeBinding>> ListAllBindingsAsync(CancellationToken ct);
}
