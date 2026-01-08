// src/Acode.Domain/Sync/SyncStatus.cs
namespace Acode.Domain.Sync;

using System;

/// <summary>
/// Represents the current status of the sync engine.
/// </summary>
public sealed class SyncStatus
{
    /// <summary>
    /// Gets a value indicating whether the sync engine is running.
    /// </summary>
    public bool IsRunning { get; init; }

    /// <summary>
    /// Gets a value indicating whether the sync engine is paused.
    /// </summary>
    public bool IsPaused { get; init; }

    /// <summary>
    /// Gets the number of pending outbox entries.
    /// </summary>
    public int PendingOutboxCount { get; init; }

    /// <summary>
    /// Gets the timestamp of the last successful sync.
    /// </summary>
    public DateTimeOffset? LastSyncAt { get; init; }

    /// <summary>
    /// Gets the timestamp when the sync engine was started.
    /// </summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    /// Gets the sync lag (age of oldest pending entry).
    /// </summary>
    public TimeSpan? SyncLag { get; init; }

    /// <summary>
    /// Gets the total number of entries processed since startup.
    /// </summary>
    public long TotalProcessed { get; init; }

    /// <summary>
    /// Gets the total number of failed entries since startup.
    /// </summary>
    public long TotalFailed { get; init; }
}
