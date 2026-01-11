// src/Acode.Application/Concurrency/IGitWorktreeDetector.cs
namespace Acode.Application.Concurrency;

using System.Threading;
using System.Threading.Tasks;
using Acode.Domain.Worktree;

/// <summary>
/// Application service for detecting Git worktrees from the filesystem.
/// Walks up directory tree looking for .git directory or worktree metadata.
/// </summary>
public interface IGitWorktreeDetector
{
    /// <summary>
    /// Detects the worktree containing the specified directory.
    /// Returns null if not in a Git worktree.
    /// </summary>
    /// <param name="currentDirectory">The directory path to start detection from.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The detected worktree information, or null if not in a worktree.</returns>
    Task<DetectedWorktree?> DetectAsync(string currentDirectory, CancellationToken ct);
}

/// <summary>
/// Result of worktree detection containing the worktree ID and root path.
/// </summary>
/// <param name="Id">The unique identifier for the worktree (derived from path).</param>
/// <param name="Path">The absolute filesystem path to the worktree root.</param>
public sealed record DetectedWorktree(WorktreeId Id, string Path);
