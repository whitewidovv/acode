// src/Acode.Application/Concurrency/LockBusyException.cs
namespace Acode.Application.Concurrency;

using System;
using Acode.Domain.Worktree;

/// <summary>
/// Exception thrown when a lock acquisition fails because the lock is held by another process.
/// </summary>
public sealed class LockBusyException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LockBusyException"/> class.
    /// </summary>
    /// <param name="worktreeId">The worktree identifier.</param>
    /// <param name="message">The exception message.</param>
    public LockBusyException(WorktreeId worktreeId, string message)
        : base(message)
    {
        WorktreeId = worktreeId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LockBusyException"/> class.
    /// </summary>
    /// <param name="worktreeId">The worktree identifier.</param>
    /// <param name="lockStatus">The current lock status.</param>
    public LockBusyException(WorktreeId worktreeId, LockStatus lockStatus)
        : base($"Worktree {worktreeId} is locked by process {lockStatus?.ProcessId} on {lockStatus?.Hostname} (age: {lockStatus?.Age})")
    {
        WorktreeId = worktreeId;
        LockStatus = lockStatus ?? throw new ArgumentNullException(nameof(lockStatus));
    }

    /// <summary>
    /// Gets the worktree identifier.
    /// </summary>
    public WorktreeId WorktreeId { get; }

    /// <summary>
    /// Gets the lock status at the time of exception (optional).
    /// </summary>
    public LockStatus? LockStatus { get; }
}
