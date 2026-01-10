// src/Acode.Infrastructure/Concurrency/SafeLockPathResolver.cs
namespace Acode.Infrastructure.Concurrency;

using System;
using System.IO;
using Acode.Domain.Worktree;
using Microsoft.Extensions.Logging;

/// <summary>
/// Safely resolves lock file paths for worktrees.
/// Prevents path traversal attacks.
/// </summary>
internal sealed class SafeLockPathResolver
{
    private readonly string _locksDirectory;
    private readonly ILogger _logger;

    public SafeLockPathResolver(string workspaceRoot, ILogger logger)
    {
        _locksDirectory = Path.Combine(workspaceRoot, ".agent", "locks");
        _logger = logger;
    }

    public string GetLockFilePath(WorktreeId worktreeId)
    {
        // Use worktreeId.Value which is already sanitized by WorktreeId factory
        var fileName = $"{worktreeId.Value}.lock";
        var fullPath = Path.Combine(_locksDirectory, fileName);

        // Verify path is still within locks directory (defense in depth)
        var normalizedPath = Path.GetFullPath(fullPath);
        var normalizedLocksDir = Path.GetFullPath(_locksDirectory);

        if (!normalizedPath.StartsWith(normalizedLocksDir, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Lock path traversal detected: {fileName}");
        }

        return fullPath;
    }
}
