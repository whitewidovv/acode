namespace Acode.Domain.Security.PathProtection;

using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Resolves symbolic links to their real target paths.
/// SECURITY: Prevents symlink bypass attacks where malicious actors
/// use symlinks to circumvent path protection.
/// </summary>
public sealed class SymlinkResolver : ISymlinkResolver
{
    private readonly int _maxDepth;
    private readonly Dictionary<string, SymlinkResolutionResult> _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="SymlinkResolver"/> class.
    /// </summary>
    /// <param name="maxDepth">Maximum symlink chain depth to follow (default: 40).</param>
    public SymlinkResolver(int maxDepth = 40)
    {
        if (maxDepth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDepth), "Max depth must be greater than zero");
        }

        _maxDepth = maxDepth;
        _cache = new Dictionary<string, SymlinkResolutionResult>(StringComparer.Ordinal);
    }

    /// <summary>
    /// Resolves a path that may be a symlink to its real target.
    /// Handles symlink chains (a → b → c) and detects circular references.
    /// </summary>
    /// <param name="path">Path that may be a symlink.</param>
    /// <returns>
    /// Resolution result containing:
    /// - ResolvedPath: Final target path if successful.
    /// - IsSuccess: True if resolution succeeded.
    /// - Error: Error code if resolution failed.
    /// - Depth: Number of symlinks traversed.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when path is null.</exception>
    public SymlinkResolutionResult Resolve(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return SymlinkResolutionResult.Failure(SymlinkError.TargetNotFound);
        }

        // Check cache first
        if (_cache.TryGetValue(path, out var cachedResult))
        {
            return cachedResult;
        }

        // Resolve the symlink
        var result = ResolveInternal(path);

        // Cache the result
        _cache[path] = result;

        return result;
    }

    private static bool IsSymlink(string path)
    {
        try
        {
            // Check if path is a reparse point (symlink, junction, etc.)
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists)
            {
                return fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
            }

            var dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists)
            {
                return dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
            }

            return false;
        }
        catch (UnauthorizedAccessException)
        {
            // Access denied - can't determine if it's a symlink
            return false;
        }
        catch (Exception)
        {
            // Other errors - assume not a symlink
            return false;
        }
    }

    private static string? GetSymlinkTarget(string path)
    {
        try
        {
            // Try as file first
            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists && fileInfo.LinkTarget != null)
            {
                return fileInfo.LinkTarget;
            }

            // Try as directory
            var dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists && dirInfo.LinkTarget != null)
            {
                return dirInfo.LinkTarget;
            }

            return null;
        }
        catch (UnauthorizedAccessException)
        {
            // Access denied
            return null;
        }
        catch (Exception)
        {
            // Other errors
            return null;
        }
    }

    private SymlinkResolutionResult ResolveInternal(string path)
    {
        // Check if path exists
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return SymlinkResolutionResult.Failure(SymlinkError.TargetNotFound);
        }

        // Track visited paths to detect circular references
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = path;
        var depth = 0;

        while (true)
        {
            // Check if we've exceeded max depth
            if (depth >= _maxDepth)
            {
                return SymlinkResolutionResult.Failure(SymlinkError.MaxDepthExceeded);
            }

            // Check if current path is a symlink
            if (!IsSymlink(current))
            {
                // Not a symlink, we're done
                return SymlinkResolutionResult.Success(current, depth);
            }

            // Check for circular reference
            if (!visited.Add(current))
            {
                return SymlinkResolutionResult.Failure(SymlinkError.CircularReference);
            }

            // Get the symlink target
            var target = GetSymlinkTarget(current);
            if (target == null)
            {
                return SymlinkResolutionResult.Failure(SymlinkError.TargetNotFound);
            }

            // Make target path absolute if it's relative
            if (!Path.IsPathRooted(target))
            {
                var directory = Path.GetDirectoryName(current);
                if (directory != null)
                {
                    target = Path.GetFullPath(Path.Combine(directory, target));
                }
            }

            // Check if target exists
            if (!File.Exists(target) && !Directory.Exists(target))
            {
                return SymlinkResolutionResult.Failure(SymlinkError.TargetNotFound);
            }

            // Move to the target
            current = target;
            depth++;
        }
    }
}
