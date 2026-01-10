using System.Text;
using System.Text.RegularExpressions;

namespace Acode.Infrastructure.Search;

/// <summary>
/// Safely parses and sanitizes user search queries for FTS5.
/// </summary>
public sealed class SafeQueryParser
{
    // FTS5 special operators that should be escaped to prevent query injection
    private static readonly string[] Fts5Operators = new[]
    {
        "AND", "OR", "NOT", "NEAR"
    };

    private static readonly char[] Fts5SpecialChars = new[]
    {
        '*', '^', '"', '(', ')', '{', '}', '[', ']', ':'
    };

    /// <summary>
    /// Parses and sanitizes a user query for safe FTS5 execution.
    /// </summary>
    /// <param name="query">The raw user query.</param>
    /// <returns>A sanitized query safe for FTS5.</returns>
    public string ParseQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return string.Empty;
        }

        // Normalize whitespace
        var normalized = NormalizeWhitespace(query);

        // Remove quotes (they can be used for phrase search but simplify for now)
        normalized = normalized.Replace("\"", string.Empty, StringComparison.Ordinal);

        // Extract alphanumeric terms and spaces
        var sanitized = SanitizeQuery(normalized);

        // Remove FTS5 operators (case-insensitive)
        sanitized = RemoveFts5Operators(sanitized);

        // Normalize whitespace again after removals
        sanitized = NormalizeWhitespace(sanitized);

        return sanitized.Trim();
    }

    /// <summary>
    /// Normalizes whitespace in the query (collapses multiple spaces to single space).
    /// </summary>
    /// <param name="query">The query to normalize.</param>
    /// <returns>Query with normalized whitespace.</returns>
    private static string NormalizeWhitespace(string query)
    {
        return Regex.Replace(query, @"\s+", " ");
    }

    /// <summary>
    /// Sanitizes the query by removing special FTS5 characters.
    /// </summary>
    /// <param name="query">The query to sanitize.</param>
    /// <returns>Sanitized query.</returns>
    private static string SanitizeQuery(string query)
    {
        var result = new StringBuilder(query.Length);

        foreach (var c in query)
        {
            // Keep alphanumeric, spaces, and common punctuation (but not FTS5 special chars)
            if (char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_')
            {
                result.Append(c);
            }
            else if (Array.IndexOf(Fts5SpecialChars, c) == -1)
            {
                // Keep other punctuation that's not FTS5-special (like commas, periods)
                // But convert to space to act as word separator
                result.Append(' ');
            }

            // FTS5 special chars are dropped entirely
        }

        return result.ToString();
    }

    /// <summary>
    /// Removes FTS5 operator keywords from the query.
    /// </summary>
    /// <param name="query">The query to process.</param>
    /// <returns>Query with operators removed.</returns>
    private static string RemoveFts5Operators(string query)
    {
        var result = query;

        foreach (var op in Fts5Operators)
        {
            // Remove operator as whole word (case-insensitive)
            var pattern = $@"\b{op}\b";
            result = Regex.Replace(result, pattern, " ", RegexOptions.IgnoreCase);
        }

        return result;
    }
}
