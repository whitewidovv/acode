// src/Acode.Domain/Concurrency/WorktreeLock.cs
namespace Acode.Domain.Concurrency;

using System;
using System.Diagnostics;
using Acode.Domain.Worktree;

/// <summary>
/// Represents metadata for a worktree lock (file-based concurrency control).
/// Immutable domain entity.
/// </summary>
public sealed class WorktreeLock
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorktreeLock"/> class.
    /// </summary>
    /// <param name="worktreeId">The worktree identifier.</param>
    /// <param name="processId">The process ID that holds the lock.</param>
    /// <param name="lockedAt">The timestamp when the lock was acquired.</param>
    /// <param name="hostname">The machine hostname where the lock was acquired.</param>
    /// <param name="terminal">The terminal/session identifier.</param>
    public WorktreeLock(
        WorktreeId worktreeId,
        int processId,
        DateTimeOffset lockedAt,
        string hostname,
        string terminal)
    {
        WorktreeId = worktreeId;
        ProcessId = processId;
        LockedAt = lockedAt;
        Hostname = hostname;
        Terminal = terminal;
    }

    /// <summary>
    /// Gets the worktree identifier.
    /// </summary>
    public WorktreeId WorktreeId { get; }

    /// <summary>
    /// Gets the process ID that holds the lock.
    /// </summary>
    public int ProcessId { get; }

    /// <summary>
    /// Gets the timestamp when the lock was acquired.
    /// </summary>
    public DateTimeOffset LockedAt { get; }

    /// <summary>
    /// Gets the machine hostname where the lock was acquired.
    /// </summary>
    public string Hostname { get; }

    /// <summary>
    /// Gets the terminal/session identifier.
    /// </summary>
    public string Terminal { get; }

    /// <summary>
    /// Gets the age of the lock (time since LockedAt).
    /// </summary>
    public TimeSpan Age => DateTimeOffset.UtcNow - LockedAt;

    /// <summary>
    /// Creates a lock for the current process.
    /// </summary>
    /// <param name="worktreeId">The worktree identifier.</param>
    /// <returns>A new WorktreeLock instance for the current process.</returns>
    public static WorktreeLock CreateForCurrentProcess(WorktreeId worktreeId)
    {
        return new WorktreeLock(
            worktreeId,
            Environment.ProcessId,
            DateTimeOffset.UtcNow,
            Environment.MachineName,
            GetTerminalId());
    }

    /// <summary>
    /// Determines whether the lock is stale based on the given threshold.
    /// </summary>
    /// <param name="threshold">The staleness threshold.</param>
    /// <returns>True if the lock age exceeds the threshold, otherwise false.</returns>
    public bool IsStale(TimeSpan threshold) => Age > threshold;

    /// <summary>
    /// Determines whether the lock is owned by the current process.
    /// </summary>
    /// <returns>True if the lock process ID matches the current process, otherwise false.</returns>
    public bool IsOwnedByCurrentProcess() => ProcessId == Environment.ProcessId;

    private static string GetTerminalId()
    {
        // Unix: Use TTY, Windows: Use session ID
        if (!OperatingSystem.IsWindows())
        {
            return Environment.GetEnvironmentVariable("TTY") ?? "/dev/ttys000";
        }

        return $"session-{Process.GetCurrentProcess().SessionId}";
    }
}
