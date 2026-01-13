namespace Acode.Domain.Security.PathProtection;

/// <summary>
/// Interface for normalizing paths for consistent matching.
/// </summary>
public interface IPathNormalizer
{
    /// <summary>
    /// Normalizes a path for consistent matching.
    /// <list type="bullet">
    /// <item>Expands ~ (home directory)</item>
    /// <item>Expands $HOME, %USERPROFILE% environment variables</item>
    /// <item>Resolves .. (parent directory) and . (current directory)</item>
    /// <item>Collapses multiple consecutive slashes (// → /)</item>
    /// <item>Converts to absolute path</item>
    /// <item>Normalizes path separators (\ → / on Unix, / → \ on Windows)</item>
    /// </list>
    /// </summary>
    /// <param name="path">Path to normalize.</param>
    /// <returns>Normalized absolute path.</returns>
    string Normalize(string path);
}
