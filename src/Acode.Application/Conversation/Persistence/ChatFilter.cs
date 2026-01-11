// src/Acode.Application/Conversation/Persistence/ChatFilter.cs
namespace Acode.Application.Conversation.Persistence;

using System;
using Acode.Domain.Worktree;

/// <summary>
/// Filter criteria for Chat queries with pagination support.
/// </summary>
public record ChatFilter
{
    /// <summary>
    /// Gets or inits the worktree ID to filter by.
    /// </summary>
    public WorktreeId? WorktreeId { get; init; }

    /// <summary>
    /// Gets or inits the minimum creation date.
    /// </summary>
    public DateTimeOffset? CreatedAfter { get; init; }

    /// <summary>
    /// Gets or inits the maximum creation date.
    /// </summary>
    public DateTimeOffset? CreatedBefore { get; init; }

    /// <summary>
    /// Gets a value indicating whether to include soft-deleted chats.
    /// </summary>
    public bool IncludeDeleted { get; init; } = false;

    /// <summary>
    /// Gets or inits the title substring to filter by (case-insensitive).
    /// </summary>
    public string? TitleContains { get; init; }

    /// <summary>
    /// Gets or inits the field to sort by.
    /// </summary>
    public ChatSortField SortBy { get; init; } = ChatSortField.UpdatedAt;

    /// <summary>
    /// Gets a value indicating whether to sort in descending order.
    /// </summary>
    public bool SortDescending { get; init; } = true;

    /// <summary>
    /// Gets or inits the page number (zero-based).
    /// </summary>
    public int Page { get; init; } = 0;

    /// <summary>
    /// Gets or inits the page size.
    /// </summary>
    public int PageSize { get; init; } = 50;
}
