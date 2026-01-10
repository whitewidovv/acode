// src/Acode.Application/Concurrency/LockCorruptedException.cs
namespace Acode.Application.Concurrency;

using System;
using Acode.Domain.Worktree;

/// <summary>
/// Exception thrown when a lock file is corrupted or ownership verification fails.
/// </summary>
public sealed class LockCorruptedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LockCorruptedException"/> class.
    /// </summary>
    /// <param name="worktreeId">The worktree identifier.</param>
    /// <param name="message">The exception message.</param>
    public LockCorruptedException(WorktreeId worktreeId, string message)
        : base(message)
    {
        WorktreeId = worktreeId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LockCorruptedException"/> class.
    /// </summary>
    /// <param name="worktreeId">The worktree identifier.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public LockCorruptedException(WorktreeId worktreeId, string message, Exception innerException)
        : base(message, innerException)
    {
        WorktreeId = worktreeId;
    }

    /// <summary>
    /// Gets the worktree identifier.
    /// </summary>
    public WorktreeId WorktreeId { get; }
}
