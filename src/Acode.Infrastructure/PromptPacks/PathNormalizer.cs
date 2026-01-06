using System.Text.RegularExpressions;
using Acode.Domain.PromptPacks;

namespace Acode.Infrastructure.PromptPacks;

/// <summary>
/// Utility for normalizing and validating file paths for security.
/// </summary>
/// <remarks>
/// Ensures paths are cross-platform compatible and prevents path traversal attacks.
/// All paths must be relative to the pack root directory.
/// </remarks>
public static partial class PathNormalizer
{
    private static readonly Regex TraversalPattern = GenerateTraversalPattern();
    private static readonly Regex AbsolutePathPattern = GenerateAbsolutePathPattern();

    /// <summary>
    /// Normalizes a path by converting backslashes to forward slashes.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path with forward slashes.</returns>
    public static string Normalize(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.Replace('\\', '/');
    }

    /// <summary>
    /// Validates that a path does not contain traversal sequences or absolute paths.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <exception cref="PathTraversalException">Thrown when path traversal is detected.</exception>
    public static void Validate(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        // Check for directory traversal (../)
        if (TraversalPattern.IsMatch(path))
        {
            throw new PathTraversalException(path);
        }

        // Check for absolute paths
        if (AbsolutePathPattern.IsMatch(path))
        {
            throw new PathTraversalException(path, $"Absolute path not allowed: '{path}'. Paths must be relative to pack root.");
        }
    }

    /// <summary>
    /// Normalizes and validates a path in one operation.
    /// </summary>
    /// <param name="path">The path to normalize and validate.</param>
    /// <returns>The normalized and validated path.</returns>
    /// <exception cref="PathTraversalException">Thrown when path traversal is detected.</exception>
    public static string NormalizeAndValidate(string path)
    {
        var normalized = Normalize(path);
        Validate(normalized);
        return normalized;
    }

    [GeneratedRegex(@"\.\.[/\\]|[/\\]\.\.|^\.\.$|^\.\.")]
    private static partial Regex GenerateTraversalPattern();

    [GeneratedRegex(@"^[a-zA-Z]:[/\\]|^/")]
    private static partial Regex GenerateAbsolutePathPattern();
}
