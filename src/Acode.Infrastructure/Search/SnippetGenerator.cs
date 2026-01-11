using System.Text.RegularExpressions;
using Acode.Domain.Configuration;

namespace Acode.Infrastructure.Search;

/// <summary>
/// Generates text snippets with highlighted search terms for search results.
/// </summary>
public sealed class SnippetGenerator
{
    private const int ContextChars = 80;

    private readonly SearchSettings _settings;
    private readonly int _maxSnippetLength;

    /// <summary>
    /// Initializes a new instance of the <see cref="SnippetGenerator"/> class.
    /// </summary>
    /// <param name="settings">Search settings for configurable snippet generation.</param>
    public SnippetGenerator(SearchSettings? settings = null)
    {
        _settings = settings ?? new SearchSettings();

        // AC-059: Validate snippet length is within bounds (50-500)
        var maxLength = _settings.SnippetMaxLength;
        if (maxLength < _settings.SnippetMinLength)
        {
            maxLength = _settings.SnippetMinLength;
        }

        if (maxLength > _settings.SnippetMaxLengthLimit)
        {
            maxLength = _settings.SnippetMaxLengthLimit;
        }

        _maxSnippetLength = maxLength;
    }

    /// <summary>
    /// Generates a snippet from content with search terms highlighted.
    /// </summary>
    /// <param name="content">The full content text.</param>
    /// <param name="query">The search query containing terms to highlight.</param>
    /// <returns>A snippet with highlighted terms wrapped in mark tags.</returns>
    public string GenerateSnippet(string content, string query)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return TruncateToMaxLength(content, 0);
        }

        var queryTerms = ExtractTerms(query);
        if (queryTerms.Count == 0)
        {
            return TruncateToMaxLength(content, 0);
        }

        // Find first match position
        var firstMatchIndex = FindFirstMatch(content, queryTerms);

        if (firstMatchIndex == -1)
        {
            // No matches found - return beginning of content
            return TruncateToMaxLength(content, 0);
        }

        // Extract snippet centered around first match
        var snippetStart = Math.Max(0, firstMatchIndex - ContextChars);
        var snippetEnd = Math.Min(content.Length, firstMatchIndex + _maxSnippetLength - ContextChars);
        var snippet = content.Substring(snippetStart, snippetEnd - snippetStart);

        // Highlight all matching terms
        snippet = HighlightTerms(snippet, queryTerms);

        // Add ellipsis if truncated
        if (snippetStart > 0)
        {
            snippet = "..." + snippet;
        }

        if (snippetEnd < content.Length)
        {
            snippet += "...";
        }

        return snippet;
    }

    /// <summary>
    /// Extracts individual terms from a query string.
    /// </summary>
    /// <param name="query">The query string.</param>
    /// <returns>List of individual terms.</returns>
    private static List<string> ExtractTerms(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<string>();
        }

        // Split on whitespace and filter empty entries
        return query.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                   .Select(t => t.Trim())
                   .Where(t => !string.IsNullOrWhiteSpace(t))
                   .ToList();
    }

    /// <summary>
    /// Finds the index of the first match of any query term in content.
    /// </summary>
    /// <param name="content">The content to search.</param>
    /// <param name="queryTerms">The terms to search for.</param>
    /// <returns>Index of first match, or -1 if no match found.</returns>
    private static int FindFirstMatch(string content, List<string> queryTerms)
    {
        var firstIndex = int.MaxValue;

        foreach (var term in queryTerms)
        {
            // Use word boundary regex to find exact word matches
            var pattern = $@"\b{Regex.Escape(term)}\b";
            var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase);

            if (match.Success && match.Index < firstIndex)
            {
                firstIndex = match.Index;
            }
        }

        return firstIndex == int.MaxValue ? -1 : firstIndex;
    }

    /// <summary>
    /// Highlights query terms in the snippet using configurable tags.
    /// </summary>
    /// <param name="snippet">The snippet text.</param>
    /// <param name="queryTerms">The terms to highlight.</param>
    /// <returns>Snippet with highlighted terms.</returns>
    /// <remarks>
    /// AC-065: Highlight tags are configurable (supports HTML, ANSI, or custom tags).
    /// </remarks>
    private string HighlightTerms(string snippet, List<string> queryTerms)
    {
        var result = snippet;

        foreach (var term in queryTerms)
        {
            // Use word boundary to match whole words only, case-insensitive
            var pattern = $@"\b({Regex.Escape(term)})\b";
            var replacement = $"{_settings.HighlightOpenTag}$1{_settings.HighlightCloseTag}";
            result = Regex.Replace(result, pattern, replacement, RegexOptions.IgnoreCase);
        }

        return result;
    }

    /// <summary>
    /// Truncates content to maximum snippet length.
    /// </summary>
    /// <param name="content">The content to truncate.</param>
    /// <param name="startIndex">The starting index.</param>
    /// <returns>Truncated content.</returns>
    private string TruncateToMaxLength(string content, int startIndex)
    {
        if (content.Length - startIndex <= _maxSnippetLength)
        {
            return content.Substring(startIndex);
        }

        var truncated = content.Substring(startIndex, _maxSnippetLength);
        return truncated + "...";
    }
}
