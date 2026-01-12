namespace Acode.Domain.Security.PathProtection;

using System;
using System.IO;
using System.Runtime.InteropServices;

/// <summary>
/// Normalizes paths for consistent matching.
/// Expands environment variables, resolves relative paths, and normalizes separators.
/// </summary>
public sealed class PathNormalizer : IPathNormalizer
{
    /// <summary>
    /// Normalizes a path for consistent matching.
    /// </summary>
    /// <param name="path">Path to normalize.</param>
    /// <returns>Normalized absolute path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when path is null.</exception>
    /// <exception cref="ArgumentException">Thrown when path contains null bytes.</exception>
    public string Normalize(string path)
    {
        // Handle null input
        if (path == null)
        {
            throw new ArgumentNullException(nameof(path));
        }

        // Handle empty input
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        // Security: Reject null bytes (can bypass path validation)
#pragma warning disable CA1307 // IndexOf(char) doesn't support StringComparison
        if (path.IndexOf('\0') >= 0)
#pragma warning restore CA1307
        {
            throw new ArgumentException("Path cannot contain null bytes", nameof(path));
        }

        var normalized = path;

        // 1. Expand environment variables (~, $HOME, %USERPROFILE%)
        normalized = ExpandEnvironmentVariables(normalized);

        // 2. Normalize path separators to platform-specific
        normalized = NormalizeSeparators(normalized);

        // 3. Convert to absolute path if relative (handles ./ and ../)
        // But only if the path isn't already absolute
        try
        {
            // Use GetFullPath to resolve relative segments (./, ../) and also
            // for already rooted paths to resolve any .. and . components.
            normalized = Path.GetFullPath(normalized);
        }
        catch (Exception)
        {
            // If GetFullPath fails (e.g., invalid characters on Windows),
            // continue with manual normalization
        }

        // 4. Normalize separators again after GetFullPath (which may convert them)
        normalized = NormalizeSeparators(normalized);

        // 5. Collapse multiple consecutive slashes
        normalized = CollapseSlashes(normalized);

        // 6. Remove trailing slash (except for root paths)
        normalized = RemoveTrailingSlash(normalized);

        return normalized;
    }

    /// <summary>
    /// Expands environment variables in the path.
    /// Handles ~, $HOME, %USERPROFILE%, and other environment variables.
    /// </summary>
    private static string ExpandEnvironmentVariables(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        // Expand tilde (~) to home directory
        if (path.StartsWith("~/") || path.StartsWith("~\\") || path == "~")
        {
            var home = GetHomeDirectory();
            if (path.Length == 1)
            {
                path = home;
            }
            else
            {
                path = home + path.Substring(1);
            }
        }

        // Expand $HOME on Unix-like systems
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            path.Contains("$HOME", StringComparison.Ordinal))
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? GetHomeDirectory();
            path = path.Replace("$HOME", home, StringComparison.Ordinal);
        }

        // Expand %USERPROFILE% and other environment variables
        try
        {
            path = Environment.ExpandEnvironmentVariables(path);
        }
        catch (Exception)
        {
            // If expansion fails, continue with unexpanded path
        }

        return path;
    }

    /// <summary>
    /// Gets the user's home directory in a cross-platform way.
    /// </summary>
    private static string GetHomeDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
        else
        {
            return Environment.GetEnvironmentVariable("HOME")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
    }

    /// <summary>
    /// Normalizes path separators to the platform-specific separator.
    /// On Windows: converts / to \.
    /// On Unix: converts \ to /.
    /// </summary>
    private static string NormalizeSeparators(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        var separator = Path.DirectorySeparatorChar;
        var altSeparator = Path.AltDirectorySeparatorChar;

        if (separator == altSeparator)
        {
            // Platform uses same separator for both, no conversion needed
            return path;
        }

        // Replace alternate separator with primary separator
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? path.Replace(altSeparator, separator)
            : path.Replace('\\', '/');
    }

    /// <summary>
    /// Collapses multiple consecutive slashes into a single slash.
    /// Handles both / and \ separators.
    /// </summary>
    private static string CollapseSlashes(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        var separator = Path.DirectorySeparatorChar;
        var doubleSeparator = new string(separator, 2);
        var singleSeparator = separator.ToString();

        // Keep collapsing until no more double separators exist
        while (path.Contains(doubleSeparator, StringComparison.Ordinal))
        {
            path = path.Replace(doubleSeparator, singleSeparator, StringComparison.Ordinal);
        }

        // Also collapse any remaining // or \\ regardless of platform
        // (in case there are mixed separators)
        while (path.Contains("//", StringComparison.Ordinal))
        {
            path = path.Replace("//", "/", StringComparison.Ordinal);
        }

        while (path.Contains("\\\\", StringComparison.Ordinal))
        {
            path = path.Replace("\\\\", "\\", StringComparison.Ordinal);
        }

        return path;
    }

    /// <summary>
    /// Removes trailing slash from path, except for root paths.
    /// </summary>
    private static string RemoveTrailingSlash(string path)
    {
        if (string.IsNullOrEmpty(path) || path.Length <= 1)
        {
            return path;
        }

        // Don't remove trailing slash from root paths like "/" or "C:\"
        if (path.Length == 3 && path[1] == ':' && (path[2] == '\\' || path[2] == '/'))
        {
            // Windows root like "C:\"
            return path;
        }

        if (path.Length == 1 && (path[0] == '/' || path[0] == '\\'))
        {
            // Unix root "/"
            return path;
        }

        // Remove trailing separators
        path = path.TrimEnd('/', '\\');

        return path;
    }
}
