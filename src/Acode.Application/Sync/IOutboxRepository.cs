// src/Acode.Application/Sync/IOutboxRepository.cs
namespace Acode.Application.Sync;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Sync;

/// <summary>
/// Repository for managing outbox entries for reliable sync delivery.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Adds a new outbox entry.
    /// </summary>
    /// <param name="entry">The outbox entry to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync(OutboxEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an outbox entry by ID.
    /// </summary>
    /// <param name="id">The entry ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The outbox entry, or null if not found.</returns>
    Task<OutboxEntry?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves pending outbox entries that are ready to be processed.
    /// Excludes entries with NextRetryAt in the future.
    /// Orders by CreatedAt ascending (oldest first).
    /// </summary>
    /// <param name="limit">Maximum number of entries to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of pending outbox entries.</returns>
    Task<IReadOnlyList<OutboxEntry>> GetPendingAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing outbox entry.
    /// </summary>
    /// <param name="entry">The outbox entry to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAsync(OutboxEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an outbox entry by ID.
    /// </summary>
    /// <param name="id">The entry ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
