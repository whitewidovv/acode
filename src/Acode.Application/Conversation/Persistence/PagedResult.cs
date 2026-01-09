// src/Acode.Application/Conversation/Persistence/PagedResult.cs
namespace Acode.Application.Conversation.Persistence;

using System;
using System.Collections.Generic;

/// <summary>
/// Paginated result container with navigation metadata.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
/// <param name="Items">The items in the current page.</param>
/// <param name="TotalCount">The total number of items across all pages.</param>
/// <param name="Page">The current page number (zero-based).</param>
/// <param name="PageSize">The number of items per page.</param>
public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => Page < TotalPages - 1;

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => Page > 0;
}
