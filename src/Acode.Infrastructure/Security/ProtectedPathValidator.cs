using System.Runtime.InteropServices;
using Acode.Application.Security;
using Acode.Domain.Security.PathProtection;

namespace Acode.Infrastructure.Security;

/// <summary>
/// Validates file paths against the default denylist of protected paths.
/// Uses GlobMatcher for pattern matching, PathNormalizer for normalization,
/// and SymlinkResolver for symlink resolution to prevent bypass attacks.
/// </summary>
public sealed class ProtectedPathValidator : IProtectedPathValidator
{
    private readonly IReadOnlyList<DenylistEntry> _denylist;
    private readonly IPathMatcher _pathMatcher;
    private readonly IPathNormalizer _pathNormalizer;
    private readonly ISymlinkResolver _symlinkResolver;
    private readonly Platform _currentPlatform;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtectedPathValidator"/> class.
    /// </summary>
    /// <param name="pathMatcher">Path pattern matcher (GlobMatcher).</param>
    /// <param name="pathNormalizer">Path normalizer.</param>
    /// <param name="symlinkResolver">Symlink resolver.</param>
    public ProtectedPathValidator(
        IPathMatcher pathMatcher,
        IPathNormalizer pathNormalizer,
        ISymlinkResolver symlinkResolver)
        : this(DefaultDenylist.Entries, pathMatcher, pathNormalizer, symlinkResolver)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtectedPathValidator"/> class with custom denylist.
    /// </summary>
    /// <param name="denylist">Custom denylist entries.</param>
    /// <param name="pathMatcher">Path pattern matcher (GlobMatcher).</param>
    /// <param name="pathNormalizer">Path normalizer.</param>
    /// <param name="symlinkResolver">Symlink resolver.</param>
    public ProtectedPathValidator(
        IReadOnlyList<DenylistEntry> denylist,
        IPathMatcher pathMatcher,
        IPathNormalizer pathNormalizer,
        ISymlinkResolver symlinkResolver)
    {
        _denylist = denylist ?? throw new ArgumentNullException(nameof(denylist));
        _pathMatcher = pathMatcher ?? throw new ArgumentNullException(nameof(pathMatcher));
        _pathNormalizer = pathNormalizer ?? throw new ArgumentNullException(nameof(pathNormalizer));
        _symlinkResolver = symlinkResolver ?? throw new ArgumentNullException(nameof(symlinkResolver));
        _currentPlatform = DetectPlatform();
    }

    /// <inheritdoc/>
    public PathValidationResult Validate(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        // 1. Normalize path (expand ~, resolve .., ., etc.)
        var normalizedPath = _pathNormalizer.Normalize(path);

        // 2. Resolve symlinks to prevent bypass attacks
        var symlinkResult = _symlinkResolver.Resolve(normalizedPath);
        var realPath = symlinkResult.IsSuccess
            ? symlinkResult.ResolvedPath!
            : normalizedPath; // If symlink resolution fails, use normalized path

        // 3. Check against platform-appropriate denylist entries
        foreach (var entry in _denylist)
        {
            // Skip entries that don't apply to current platform
            if (!EntryAppliesToPlatform(entry, _currentPlatform))
            {
                continue;
            }

            // SECURITY FIX: Expand environment variables in patterns (~ %VAR%)
            // but preserve trailing slashes. Patterns like "/etc/" or "**/secrets/"
            // have semantic meaning with trailing slashes (directory prefix match).
            // Full normalization would strip trailing slashes and break protection.
            var expandedPattern = ExpandPatternEnvironmentVariables(entry.Pattern);

            // Use GlobMatcher for pattern matching (expanded pattern vs normalized path)
            if (_pathMatcher.Matches(expandedPattern, realPath))
            {
                return PathValidationResult.Blocked(entry);
            }
        }

        return PathValidationResult.Allowed();
    }

    /// <inheritdoc/>
    public PathValidationResult Validate(string path, FileOperation operation)
    {
        // For now, operation doesn't affect validation
        // Future enhancement: different rules per operation type
        return Validate(path);
    }

    private static Platform DetectPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Platform.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return Platform.Linux;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return Platform.MacOS;
        }

        // Default to All if unknown
        return Platform.All;
    }

    private static bool EntryAppliesToPlatform(DenylistEntry entry, Platform currentPlatform)
    {
        // Entry applies if it targets All platforms or the current platform
        return entry.Platforms.Contains(Platform.All) ||
               entry.Platforms.Contains(currentPlatform);
    }

    /// <summary>
    /// Expands environment variables in a pattern while preserving trailing slashes.
    /// SECURITY: Preserves trailing slashes which have semantic meaning for directory
    /// prefix matching (e.g., "/etc/" means "match everything under /etc").
    /// </summary>
    /// <param name="pattern">Pattern that may contain ~ or %VAR% environment variables.</param>
    /// <returns>Pattern with environment variables expanded, trailing slashes preserved.</returns>
    private static string ExpandPatternEnvironmentVariables(string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return pattern;
        }

        // Remember if pattern had trailing slash
        var hadTrailingSlash = pattern.EndsWith('/') || pattern.EndsWith('\\');

        var expanded = pattern;

        // Expand tilde (~) to home directory
        if (expanded.StartsWith("~/") || expanded.StartsWith("~\\") || expanded == "~")
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            expanded = expanded.Length == 1
                ? home
                : home + expanded.Substring(1);
        }

        // Expand $HOME on Unix-like systems
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            expanded.Contains("$HOME", StringComparison.Ordinal))
        {
            var home = Environment.GetEnvironmentVariable("HOME")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            expanded = expanded.Replace("$HOME", home, StringComparison.Ordinal);
        }

        // Expand %VARNAME% environment variables
        try
        {
            expanded = Environment.ExpandEnvironmentVariables(expanded);
        }
        catch (Exception)
        {
            // If expansion fails, continue with unexpanded pattern
        }

        // Normalize separators to platform-specific
        var separator = Path.DirectorySeparatorChar;
        var altSeparator = Path.AltDirectorySeparatorChar;
        if (separator != altSeparator)
        {
            expanded = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? expanded.Replace(altSeparator, separator)
                : expanded.Replace('\\', '/');
        }

        // SECURITY: Restore trailing slash if original had one
        // Trailing slashes indicate "match this directory and everything inside"
        if (hadTrailingSlash && !expanded.EndsWith('/') && !expanded.EndsWith('\\'))
        {
            expanded += separator;
        }

        return expanded;
    }
}
