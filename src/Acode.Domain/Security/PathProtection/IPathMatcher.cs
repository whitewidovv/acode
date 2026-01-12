namespace Acode.Domain.Security.PathProtection;

/// <summary>
/// Interface for matching paths against glob patterns.
/// Implementations must use linear-time algorithms to prevent ReDoS attacks.
/// </summary>
public interface IPathMatcher
{
    /// <summary>
    /// Matches a normalized path against a glob pattern.
    /// Must use linear-time algorithm to prevent ReDoS.
    /// </summary>
    /// <param name="pattern">
    /// Glob pattern supporting:
    /// <list type="bullet">
    /// <item>* - matches any characters except /</item>
    /// <item>** - matches across directories (recursive)</item>
    /// <item>? - matches single character</item>
    /// <item>[abc] - matches any character in set</item>
    /// <item>[!abc] - matches any character NOT in set</item>
    /// <item>[a-z] - matches any character in range</item>
    /// </list>
    /// </param>
    /// <param name="path">Normalized path to check.</param>
    /// <returns>True if path matches pattern.</returns>
    bool Matches(string pattern, string path);
}
