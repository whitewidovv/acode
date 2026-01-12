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

            // Normalize the pattern as well
            var normalizedPattern = _pathNormalizer.Normalize(entry.Pattern);

            // Use GlobMatcher for pattern matching
            if (_pathMatcher.Matches(normalizedPattern, realPath))
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
}
