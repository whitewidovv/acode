namespace Acode.Domain.Security.PathProtection;

/// <summary>
/// Matches paths against glob patterns using linear-time algorithm.
/// SECURITY CRITICAL: Must not use backtracking regex to prevent ReDoS attacks.
/// Supports: *, **, ?, [abc], [!abc], [a-z].
/// </summary>
public sealed class GlobMatcher : IPathMatcher
{
    private readonly bool _caseSensitive;
    private readonly StringComparison _comparison;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobMatcher"/> class.
    /// </summary>
    /// <param name="caseSensitive">Whether to use case-sensitive matching (Unix: true, Windows: false).</param>
    public GlobMatcher(bool caseSensitive)
    {
        _caseSensitive = caseSensitive;
        _comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    }

    /// <summary>
    /// Matches a normalized path against a glob pattern.
    /// Uses linear-time algorithm to prevent ReDoS.
    /// </summary>
    /// <param name="pattern">Glob pattern.</param>
    /// <param name="path">Normalized path to check.</param>
    /// <returns>True if path matches pattern.</returns>
    public bool Matches(string pattern, string path)
    {
        // Normalize slashes
        pattern = NormalizeSlashes(pattern);
        path = NormalizeSlashes(path);

        // Handle directory prefix patterns (ending with /)
        if (pattern.EndsWith('/') && !path.EndsWith('/'))
        {
            // Pattern is "~/.ssh/" and path is "~/.ssh/id_rsa"
            // Should match if path starts with pattern
            return path.StartsWith(pattern, _comparison) || path.StartsWith(pattern.TrimEnd('/'), _comparison);
        }

        // Handle trailing slash variations (pattern "~/.ssh/" should match path "~/.ssh")
        if (pattern.EndsWith('/') && path.EndsWith('/'))
        {
            pattern = pattern.TrimEnd('/');
            path = path.TrimEnd('/');
        }
        else if (!pattern.EndsWith('/') && path.EndsWith('/'))
        {
            path = path.TrimEnd('/');
        }

        // Use linear-time glob matching algorithm
        return MatchGlob(pattern, path, 0, 0);
    }

    /// <summary>
    /// Normalizes slashes by replacing multiple consecutive slashes with a single slash.
    /// </summary>
    private string NormalizeSlashes(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        // Replace multiple consecutive slashes with single slash
        var result = new System.Text.StringBuilder(str.Length);
        bool lastWasSlash = false;

        foreach (char c in str)
        {
            if (c == '/' || c == '\\')
            {
                if (!lastWasSlash)
                {
                    result.Append('/');
                    lastWasSlash = true;
                }
            }
            else
            {
                result.Append(c);
                lastWasSlash = false;
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Linear-time glob matching algorithm using recursion without backtracking.
    /// Handles *, **, ?, [abc], [!abc], [a-z].
    /// </summary>
    /// <param name="pattern">Glob pattern.</param>
    /// <param name="path">Path to match.</param>
    /// <param name="pi">Current position in pattern.</param>
    /// <param name="si">Current position in path.</param>
    /// <returns>True if the remaining pattern matches the remaining path.</returns>
    private bool MatchGlob(string pattern, string path, int pi, int si)
    {
        int plen = pattern.Length;
        int slen = path.Length;

        // Base case: both pattern and path exhausted
        if (pi == plen && si == slen)
        {
            return true;
        }

        // Pattern exhausted but path remains
        if (pi == plen)
        {
            return false;
        }

        // Check for ** (recursive wildcard)
        if (pi + 1 < plen && pattern[pi] == '*' && pattern[pi + 1] == '*')
        {
            // Skip past all consecutive * (including **)
            while (pi < plen && pattern[pi] == '*')
            {
                pi++;
            }

            // Skip optional /
            if (pi < plen && pattern[pi] == '/')
            {
                pi++;
            }

            // ** matches zero or more path segments
            // Try matching from current position onwards
            for (int i = si; i <= slen; i++)
            {
                if (MatchGlob(pattern, path, pi, i))
                {
                    return true;
                }

                // Move to next path segment
                if (i < slen && path[i] == '/')
                {
                    continue;
                }
            }

            return false;
        }

        // Path exhausted but pattern remains (could still match if pattern is *)
        if (si == slen)
        {
            // Check if remaining pattern is all *
            while (pi < plen && pattern[pi] == '*')
            {
                pi++;
            }

            return pi == plen;
        }

        char pc = pattern[pi];

        // Handle single character wildcard (?)
        if (pc == '?')
        {
            return MatchGlob(pattern, path, pi + 1, si + 1);
        }

        // Handle single-segment wildcard (*)
        if (pc == '*')
        {
            // * matches ONE or more characters EXCEPT /
            // Note: * must match at least one character (not zero)
            // Try matching from current position onwards, stopping at /
            for (int i = si; i <= slen; i++)
            {
                // Skip zero-length match (i == si means * matched nothing)
                if (i == si)
                {
                    continue;
                }

                if (i < slen && path[i] == '/')
                {
                    // Stop at directory separator
                    break;
                }

                if (MatchGlob(pattern, path, pi + 1, i))
                {
                    return true;
                }
            }

            return false;
        }

        // Handle character class [...] or [!...]
        if (pc == '[')
        {
            int closeIdx = pattern.IndexOf(']', pi + 1);
            if (closeIdx == -1)
            {
                // Invalid pattern, treat [ as literal
                return CharMatches(pc, path[si]) && MatchGlob(pattern, path, pi + 1, si + 1);
            }

            string charClass = pattern.Substring(pi + 1, closeIdx - pi - 1);
            bool negate = charClass.StartsWith('!');
            if (negate)
            {
                charClass = charClass.Substring(1);
            }

            bool matches = MatchesCharClass(path[si], charClass);
            if (negate)
            {
                matches = !matches;
            }

            if (!matches)
            {
                return false;
            }

            return MatchGlob(pattern, path, closeIdx + 1, si + 1);
        }

        // Handle literal character
        if (!CharMatches(pc, path[si]))
        {
            return false;
        }

        return MatchGlob(pattern, path, pi + 1, si + 1);
    }

    /// <summary>
    /// Checks if a character matches a character class (e.g., "abc" or "a-z").
    /// </summary>
    private bool MatchesCharClass(char c, string charClass)
    {
        for (int i = 0; i < charClass.Length; i++)
        {
            // Check for range (a-z)
            if (i + 2 < charClass.Length && charClass[i + 1] == '-')
            {
                char start = charClass[i];
                char end = charClass[i + 2];

                if (CharInRange(c, start, end))
                {
                    return true;
                }

                i += 2; // Skip past range
                continue;
            }

            // Check for exact match
            if (CharMatches(charClass[i], c))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a character is in a range (e.g., 'a' to 'z').
    /// </summary>
    private bool CharInRange(char c, char start, char end)
    {
        if (_caseSensitive)
        {
            return c >= start && c <= end;
        }

        // Case-insensitive range check
        char cLower = char.ToLowerInvariant(c);
        char startLower = char.ToLowerInvariant(start);
        char endLower = char.ToLowerInvariant(end);

        return cLower >= startLower && cLower <= endLower;
    }

    /// <summary>
    /// Checks if two characters match (respecting case sensitivity).
    /// </summary>
    private bool CharMatches(char a, char b)
    {
        if (_caseSensitive)
        {
            return a == b;
        }

        return char.ToLowerInvariant(a) == char.ToLowerInvariant(b);
    }
}
