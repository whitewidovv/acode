using Acode.Application.Security;
using Acode.Domain.Security.PathProtection;

namespace Acode.Infrastructure.Security;

/// <summary>
/// Validates file paths against the default denylist of protected paths.
/// </summary>
public sealed class ProtectedPathValidator : IProtectedPathValidator
{
    private readonly IReadOnlyList<DenylistEntry> _denylist;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtectedPathValidator"/> class.
    /// </summary>
    public ProtectedPathValidator()
    {
        _denylist = DefaultDenylist.Entries;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtectedPathValidator"/> class with custom denylist.
    /// </summary>
    /// <param name="denylist">Custom denylist entries.</param>
    public ProtectedPathValidator(IReadOnlyList<DenylistEntry> denylist)
    {
        _denylist = denylist ?? throw new ArgumentNullException(nameof(denylist));
    }

    /// <inheritdoc/>
    public PathValidationResult Validate(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        // Check against denylist patterns
        foreach (var entry in _denylist)
        {
            if (PathMatchesPattern(path, entry.Pattern))
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

    private static bool PathMatchesPattern(string path, string pattern)
    {
        // Normalize path separators
        var normalizedPath = path.Replace('\\', '/');
        var normalizedPattern = pattern.Replace('\\', '/');

        // Handle tilde expansion
        if (normalizedPattern.StartsWith("~/", StringComparison.Ordinal))
        {
            normalizedPattern = normalizedPattern[2..]; // Remove ~/
        }

        // Handle **/ prefix (matches any depth)
        if (normalizedPattern.StartsWith("**/", StringComparison.Ordinal))
        {
            normalizedPattern = normalizedPattern[3..]; // Remove **/
            return normalizedPath.Contains(normalizedPattern, StringComparison.OrdinalIgnoreCase) ||
                   normalizedPath.EndsWith(normalizedPattern, StringComparison.OrdinalIgnoreCase);
        }

        // Handle * wildcard
        if (normalizedPattern.Contains('*', StringComparison.Ordinal))
        {
            // Simple wildcard matching
            return SimpleWildcardMatch(normalizedPath, normalizedPattern);
        }

        // Exact match or starts-with for directory patterns
        if (normalizedPattern.EndsWith('/'))
        {
            return normalizedPath.StartsWith(normalizedPattern, StringComparison.OrdinalIgnoreCase) ||
                   normalizedPath.Equals(normalizedPattern.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
        }

        // Check if path contains or equals the pattern
        return normalizedPath.Contains(normalizedPattern, StringComparison.OrdinalIgnoreCase) ||
               normalizedPath.Equals(normalizedPattern, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SimpleWildcardMatch(string path, string pattern)
    {
        // Split on * and check if all parts exist in order
        var parts = pattern.Split('*', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return true; // Pattern is just "*"
        }

        var currentIndex = 0;
        foreach (var part in parts)
        {
            var index = path.IndexOf(part, currentIndex, StringComparison.OrdinalIgnoreCase);
            if (index == -1)
            {
                return false;
            }

            currentIndex = index + part.Length;
        }

        return true;
    }
}
