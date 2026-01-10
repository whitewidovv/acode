// src/Acode.Application/Concurrency/ILockService.cs
namespace Acode.Application.Concurrency;

using System;
using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Worktree;

/// <summary>
/// Application service for managing file-based worktree locks.
/// Provides atomic lock acquisition, stale detection, and cleanup.
/// </summary>
public interface ILockService
{
    /// <summary>
    /// Acquires a lock on the specified worktree.
    /// Returns a disposable handle that releases the lock when disposed.
    /// </summary>
    /// <param name="worktreeId">The worktree identifier.</param>
    /// <param name="timeout">Optional timeout for lock acquisition. If null, no timeout.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async disposable handle that releases the lock when disposed.</returns>
    /// <exception cref="LockBusyException">Thrown if lock is held by another process and timeout expires.</exception>
    /// <exception cref="LockCorruptedException">Thrown if lock file is corrupted or ownership verification fails.</exception>
    Task<IAsyncDisposable> AcquireAsync(
        WorktreeId worktreeId,
        TimeSpan? timeout,
        CancellationToken ct);

    /// <summary>
    /// Gets the current lock status for the specified worktree.
    /// </summary>
    /// <param name="worktreeId">The worktree identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The lock status information.</returns>
    Task<LockStatus> GetStatusAsync(
        WorktreeId worktreeId,
        CancellationToken ct);

    /// <summary>
    /// Releases all stale locks (locks older than the threshold).
    /// Typically used for cleanup operations.
    /// </summary>
    /// <param name="threshold">Age threshold for stale detection (e.g., 5 minutes).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ReleaseStaleLocksAsync(
        TimeSpan threshold,
        CancellationToken ct);

    /// <summary>
    /// Forcefully unlocks a worktree regardless of current lock state.
    /// Use with caution - only for emergency recovery.
    /// </summary>
    /// <param name="worktreeId">The worktree identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ForceUnlockAsync(
        WorktreeId worktreeId,
        CancellationToken ct);
}

/// <summary>
/// Represents the current lock status for a worktree.
/// Immutable record type.
/// </summary>
/// <param name="IsLocked">True if the worktree is currently locked.</param>
/// <param name="IsStale">True if the lock is stale (older than threshold).</param>
/// <param name="Age">The age of the lock (time since acquisition).</param>
/// <param name="ProcessId">The process ID holding the lock (null if not locked).</param>
/// <param name="Hostname">The hostname where the lock was acquired (null if not locked).</param>
/// <param name="Terminal">The terminal/session identifier (null if not locked).</param>
public sealed record LockStatus(
    bool IsLocked,
    bool IsStale,
    TimeSpan Age,
    int? ProcessId,
    string? Hostname,
    string? Terminal);
