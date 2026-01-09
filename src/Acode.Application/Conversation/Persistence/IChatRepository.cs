// src/Acode.Application/Conversation/Persistence/IChatRepository.cs
namespace Acode.Application.Conversation.Persistence;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Conversation;
using Acode.Domain.Worktree;

/// <summary>
/// Repository interface for Chat aggregate root persistence.
/// Provides CRUD operations with filtering, pagination, and soft delete support.
/// </summary>
public interface IChatRepository
{
    /// <summary>
    /// Creates a new Chat and returns its ID.
    /// </summary>
    /// <param name="chat">The chat to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created chat ID.</returns>
    Task<ChatId> CreateAsync(Chat chat, CancellationToken ct);

    /// <summary>
    /// Gets a Chat by ID, optionally including its Runs.
    /// </summary>
    /// <param name="id">The chat ID.</param>
    /// <param name="includeRuns">Whether to include related runs.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The chat if found, otherwise null.</returns>
    Task<Chat?> GetByIdAsync(ChatId id, bool includeRuns = false, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing Chat.
    /// </summary>
    /// <param name="chat">The chat to update.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ConcurrencyException">Thrown if version conflict occurs.</exception>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(Chat chat, CancellationToken ct);

    /// <summary>
    /// Soft deletes a Chat by setting IsDeleted flag.
    /// </summary>
    /// <param name="id">The chat ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SoftDeleteAsync(ChatId id, CancellationToken ct);

    /// <summary>
    /// Lists Chats with filtering and pagination.
    /// </summary>
    /// <param name="filter">The filter criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paged result of chats.</returns>
    Task<PagedResult<Chat>> ListAsync(ChatFilter filter, CancellationToken ct);

    /// <summary>
    /// Gets Chats bound to a specific worktree.
    /// </summary>
    /// <param name="worktreeId">The worktree ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of chats bound to the worktree.</returns>
    Task<IReadOnlyList<Chat>> GetByWorktreeAsync(WorktreeId worktreeId, CancellationToken ct);

    /// <summary>
    /// Permanently deletes Chats marked deleted before the cutoff date.
    /// </summary>
    /// <param name="before">The cutoff date.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of chats permanently deleted.</returns>
    Task<int> PurgeDeletedAsync(DateTimeOffset before, CancellationToken ct);
}
