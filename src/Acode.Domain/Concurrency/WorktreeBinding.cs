// src/Acode.Domain/Concurrency/WorktreeBinding.cs
namespace Acode.Domain.Concurrency;

using System;
using Acode.Domain.Conversation;
using Acode.Domain.Worktree;

/// <summary>
/// Represents a one-to-one binding between a Git worktree and a chat conversation.
/// Immutable domain entity.
/// </summary>
public sealed class WorktreeBinding
{
    private WorktreeBinding(
        WorktreeId worktreeId,
        ChatId chatId,
        DateTimeOffset createdAt)
    {
        WorktreeId = worktreeId;
        ChatId = chatId;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Gets the worktree identifier.
    /// </summary>
    public WorktreeId WorktreeId { get; }

    /// <summary>
    /// Gets the chat identifier.
    /// </summary>
    public ChatId ChatId { get; }

    /// <summary>
    /// Gets the timestamp when the binding was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Creates a new worktree-to-chat binding with current timestamp.
    /// </summary>
    /// <param name="worktreeId">The worktree identifier.</param>
    /// <param name="chatId">The chat identifier.</param>
    /// <returns>A new WorktreeBinding instance.</returns>
    public static WorktreeBinding Create(WorktreeId worktreeId, ChatId chatId)
    {
        return new WorktreeBinding(worktreeId, chatId, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Reconstitutes a binding from stored values (for persistence layer).
    /// </summary>
    /// <param name="worktreeId">The worktree identifier.</param>
    /// <param name="chatId">The chat identifier.</param>
    /// <param name="createdAt">The original creation timestamp.</param>
    /// <returns>A reconstituted WorktreeBinding instance.</returns>
    public static WorktreeBinding Reconstitute(
        WorktreeId worktreeId,
        ChatId chatId,
        DateTimeOffset createdAt)
    {
        return new WorktreeBinding(worktreeId, chatId, createdAt);
    }
}
