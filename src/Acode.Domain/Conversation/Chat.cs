// src/Acode.Domain/Conversation/Chat.cs
namespace Acode.Domain.Conversation;

using System;
using System.Collections.Generic;
using Acode.Domain.Common;
using Acode.Domain.Worktree;

/// <summary>
/// Chat aggregate root representing a conversation thread.
/// Contains one or more Runs, each with multiple Messages.
/// </summary>
public sealed class Chat : AggregateRoot<ChatId>
{
    private const int MaxTitleLength = 500;
    private readonly List<string> _tags = new();

    // Private constructor for ORM/deserialization
    private Chat()
    {
    }

    private Chat(ChatId id, string title, WorktreeId? worktreeId, DateTimeOffset createdAt)
    {
        Id = id;
        Title = title;
        WorktreeBinding = worktreeId;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        IsDeleted = false;
        SyncStatus = SyncStatus.Pending;
        Version = 1;
    }

    /// <summary>
    /// Gets the chat title.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the tags associated with this chat.
    /// </summary>
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();

    /// <summary>
    /// Gets the worktree binding (nullable).
    /// </summary>
    public WorktreeId? WorktreeBinding { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this chat is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Gets the deletion timestamp if deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>
    /// Gets the sync status.
    /// </summary>
    public SyncStatus SyncStatus { get; private set; }

    /// <summary>
    /// Gets the version for optimistic concurrency.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the last update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Creates a new Chat with generated ID and current timestamp.
    /// </summary>
    /// <param name="title">The chat title.</param>
    /// <param name="worktreeId">Optional worktree binding.</param>
    /// <returns>A new Chat instance.</returns>
    public static Chat Create(string title, WorktreeId? worktreeId = null)
    {
        ValidateTitle(title);
        return new Chat(ChatId.NewId(), title, worktreeId, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Reconstitutes a Chat from persisted data.
    /// </summary>
    /// <param name="id">The chat ID.</param>
    /// <param name="title">The chat title.</param>
    /// <param name="tags">The tags collection.</param>
    /// <param name="worktreeId">Optional worktree binding.</param>
    /// <param name="isDeleted">Whether the chat is deleted.</param>
    /// <param name="deletedAt">The deletion timestamp.</param>
    /// <param name="syncStatus">The sync status.</param>
    /// <param name="version">The version number.</param>
    /// <param name="createdAt">The creation timestamp.</param>
    /// <param name="updatedAt">The last update timestamp.</param>
    /// <returns>A reconstituted Chat instance.</returns>
    public static Chat Reconstitute(
        ChatId id,
        string title,
        IEnumerable<string> tags,
        WorktreeId? worktreeId,
        bool isDeleted,
        DateTimeOffset? deletedAt,
        SyncStatus syncStatus,
        int version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        var chat = new Chat
        {
            Id = id,
            Title = title,
            WorktreeBinding = worktreeId,
            IsDeleted = isDeleted,
            DeletedAt = deletedAt,
            SyncStatus = syncStatus,
            Version = version,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };
        chat._tags.AddRange(tags);
        return chat;
    }

    /// <summary>
    /// Updates the chat title. Increments version.
    /// </summary>
    /// <param name="newTitle">The new title.</param>
    public void UpdateTitle(string newTitle)
    {
        ValidateTitle(newTitle);

        if (IsDeleted)
        {
            throw new InvalidOperationException("Cannot update deleted chat");
        }

        Title = newTitle;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Adds a tag to the chat. Ignores duplicates.
    /// </summary>
    /// <param name="tag">The tag to add.</param>
    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new ArgumentException("Tag cannot be empty", nameof(tag));
        }

        var normalizedTag = tag.Trim().ToLowerInvariant();

        if (!_tags.Contains(normalizedTag))
        {
            _tags.Add(normalizedTag);
            UpdatedAt = DateTimeOffset.UtcNow;
            Version++;
            SyncStatus = SyncStatus.Pending;
        }
    }

    /// <summary>
    /// Removes a tag from the chat.
    /// </summary>
    /// <param name="tag">The tag to remove.</param>
    /// <returns>True if the tag was removed; otherwise false.</returns>
    public bool RemoveTag(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        var normalizedTag = tag.Trim().ToLowerInvariant();
        var removed = _tags.Remove(normalizedTag);

        if (removed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            Version++;
            SyncStatus = SyncStatus.Pending;
        }

        return removed;
    }

    /// <summary>
    /// Binds chat to a specific worktree.
    /// </summary>
    /// <param name="worktreeId">The worktree ID.</param>
    public void BindToWorktree(WorktreeId worktreeId)
    {
        WorktreeBinding = worktreeId;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Soft deletes the chat. Sets IsDeleted and DeletedAt.
    /// </summary>
    public void Delete()
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Restores a soft-deleted chat.
    /// </summary>
    public void Restore()
    {
        if (!IsDeleted)
        {
            throw new InvalidOperationException("Chat is not deleted");
        }

        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
        SyncStatus = SyncStatus.Pending;
    }

    /// <summary>
    /// Marks the chat as synced to remote.
    /// </summary>
    public void MarkSynced()
    {
        SyncStatus = SyncStatus.Synced;
    }

    /// <summary>
    /// Marks the chat as having a sync conflict.
    /// </summary>
    public void MarkConflict()
    {
        SyncStatus = SyncStatus.Conflict;
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title cannot be empty", nameof(title));
        }

        if (title.Length > MaxTitleLength)
        {
            throw new ArgumentException($"Title cannot exceed {MaxTitleLength} characters", nameof(title));
        }
    }
}
