// src/Acode.Infrastructure/Concurrency/GitWorktreeDetector.cs
namespace Acode.Infrastructure.Concurrency;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Concurrency;
using Acode.Domain.Worktree;
using Microsoft.Extensions.Logging;

/// <summary>
/// Infrastructure implementation of <see cref="IGitWorktreeDetector"/>.
/// Detects Git worktrees by walking up the directory tree looking for .git directory or file.
/// </summary>
public sealed class GitWorktreeDetector : IGitWorktreeDetector
{
    private readonly ILogger<GitWorktreeDetector> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitWorktreeDetector"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public GitWorktreeDetector(ILogger<GitWorktreeDetector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Task<DetectedWorktree?> DetectAsync(string currentDirectory, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(currentDirectory))
        {
            return Task.FromResult<DetectedWorktree?>(null);
        }

        // Normalize the path
        string? directory;
        try
        {
            directory = Path.GetFullPath(currentDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to normalize path: {Path}", currentDirectory);
            return Task.FromResult<DetectedWorktree?>(null);
        }

        // Check if directory exists
        if (!Directory.Exists(directory))
        {
            _logger.LogDebug("Directory does not exist: {Path}", directory);
            return Task.FromResult<DetectedWorktree?>(null);
        }

        // Walk up the directory tree
        while (directory is not null)
        {
            ct.ThrowIfCancellationRequested();

            var gitPath = Path.Combine(directory, ".git");

            // Check if .git exists (directory or file)
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                _logger.LogDebug("Found Git worktree at: {Path}", directory);

                var worktreeId = WorktreeId.FromPath(directory);
                return Task.FromResult<DetectedWorktree?>(new DetectedWorktree(worktreeId, directory));
            }

            // Move up to parent directory
            var parentDirectory = Directory.GetParent(directory)?.FullName;

            // Stop if we've reached the root or can't go higher
            if (parentDirectory == null || parentDirectory == directory)
            {
                break;
            }

            directory = parentDirectory;
        }

        _logger.LogDebug("No Git worktree found for: {Path}", currentDirectory);
        return Task.FromResult<DetectedWorktree?>(null);
    }
}
