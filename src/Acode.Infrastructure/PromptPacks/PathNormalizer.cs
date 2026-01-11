using Acode.Domain.PromptPacks.Exceptions;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Provides path normalization and security validation for prompt pack paths.
/// </summary>
public static class PathNormalizer
{
    /// <summary>
    /// Normalizes a path by converting to forward slashes and resolving relative segments.
    /// Throws for invalid paths including traversal attempts and absolute paths.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    /// <exception cref="ArgumentException">Thrown when path is null, whitespace, or absolute.</exception>
    /// <exception cref="PathTraversalException">Thrown when path contains traversal sequences.</exception>
    public static string Normalize(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        // Check for absolute paths first (before normalization)
        if (path.StartsWith('/')
            || path.StartsWith('\\')
            || (path.Length >= 2 && path[1] == ':'))
        {
            throw new ArgumentException($"Path '{path}' is absolute. Only relative paths are allowed.", nameof(path));
        }

        // Convert backslashes to forward slashes
        var normalized = path.Replace('\\', '/');

        // Collapse multiple slashes
        while (normalized.Contains("//", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("//", "/", StringComparison.Ordinal);
        }

        // Remove trailing slashes
        normalized = normalized.TrimEnd('/');

        // Remove ./ segments
        while (normalized.Contains("/./", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("/./", "/", StringComparison.Ordinal);
        }

        if (normalized.StartsWith("./", StringComparison.Ordinal))
        {
            normalized = normalized[2..];
        }

        // Check for path traversal attempts (..)
        if (normalized == ".."
            || normalized.Contains("/..", StringComparison.Ordinal)
            || normalized.StartsWith("../", StringComparison.Ordinal)
            || normalized.EndsWith("/..", StringComparison.Ordinal))
        {
            throw new PathTraversalException(
                "ACODE-PKL-007",
                $"Path '{path}' contains invalid path traversal sequences.");
        }

        return normalized;
    }

    /// <summary>
    /// Checks whether a path is safe (does not attempt directory traversal).
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns><c>true</c> if the path is safe; otherwise, <c>false</c>.</returns>
    public static bool IsPathSafe(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            Normalize(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks whether a path stays within the specified root directory.
    /// </summary>
    /// <param name="root">The root directory path.</param>
    /// <param name="path">The relative path to validate.</param>
    /// <returns><c>true</c> if the path is safe and stays within the root; otherwise, <c>false</c>.</returns>
    public static bool IsPathSafe(string root, string path)
    {
        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            var normalized = Normalize(path);

            // Combine root and path and ensure result is under root
            var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
            var normalizedRoot = Path.GetFullPath(root);

            return fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates that a path is safe and throws if it is not.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <exception cref="PathTraversalException">Thrown when the path contains traversal attempts.</exception>
    public static void EnsurePathSafe(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new PathTraversalException(
                "ACODE-PKL-007",
                "Path cannot be null or whitespace.");
        }

        Normalize(path);
    }
}
