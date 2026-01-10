// src/Acode.Application/Concurrency/IContextResolver.cs
namespace Acode.Application.Concurrency;

using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Conversation;
using Acode.Domain.Worktree;

/// <summary>
/// Application service for resolving active chat context based on current worktree.
/// Supports automatic chat switching when user changes directories between worktrees.
/// </summary>
public interface IContextResolver
{
    /// <summary>
    /// Resolves the active chat for the specified worktree.
    /// Returns the bound chat if binding exists, null otherwise.
    /// </summary>
    /// <param name="currentWorktree">The current worktree identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The active chat ID, or null if no binding exists.</returns>
    Task<ChatId?> ResolveActiveChatAsync(
        WorktreeId currentWorktree,
        CancellationToken ct);

    /// <summary>
    /// Detects the current worktree based on the working directory.
    /// Walks up directory tree looking for .git directory or worktree metadata.
    /// </summary>
    /// <param name="currentDirectory">The current working directory path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The detected worktree ID, or null if not in a worktree.</returns>
    Task<WorktreeId?> DetectCurrentWorktreeAsync(
        string currentDirectory,
        CancellationToken ct);

    /// <summary>
    /// Notifies the context resolver of a worktree switch.
    /// Allows for logging, metrics, or other side effects when switching worktrees.
    /// </summary>
    /// <param name="from">The previous worktree identifier.</param>
    /// <param name="toWorktree">The new worktree identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task NotifyContextSwitchAsync(
        WorktreeId from,
        WorktreeId toWorktree,
        CancellationToken ct);
}
