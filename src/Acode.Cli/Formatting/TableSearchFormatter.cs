// src/Acode.Cli/Formatting/TableSearchFormatter.cs
namespace Acode.Cli.Formatting;

using Acode.Domain.Search;

/// <summary>
/// Formats search results as a table with ANSI color highlighting.
/// </summary>
public sealed class TableSearchFormatter : IOutputFormatter
{
    private const int ScoreWidth = 6;
    private const int ChatWidth = 20;
    private const int DateWidth = 11;
    private const int RoleWidth = 8;
    private const int DefaultTerminalWidth = 120;

    // ANSI color codes
    private const string YellowBackground = "\x1b[43m\x1b[30m"; // Yellow background, black text
    private const string ResetColor = "\x1b[0m";

    /// <inheritdoc/>
    public void WriteSearchResults(SearchResults results, TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(output);

        if (results.Results.Count == 0)
        {
            output.WriteLine("No results found.");
            return;
        }

        // Calculate snippet width based on terminal width
        var snippetWidth = DefaultTerminalWidth - ScoreWidth - ChatWidth - DateWidth - RoleWidth - 10; // 10 for spacing

        // Write header
        output.WriteLine($"{"SCORE",-ScoreWidth}  {"CHAT",-ChatWidth}  {"DATE",-DateWidth}  {"ROLE",-RoleWidth}  {"SNIPPET"}");
        output.WriteLine($"{new string('-', ScoreWidth)}  {new string('-', ChatWidth)}  {new string('-', DateWidth)}  {new string('-', RoleWidth)}  {new string('-', snippetWidth)}");

        // Write rows
        foreach (var result in results.Results)
        {
            var score = result.Score.ToString("F2").PadLeft(ScoreWidth);
            var chat = TruncateString(result.ChatTitle, ChatWidth);
            var date = result.CreatedAt.ToString("yyyy-MM-dd").PadRight(DateWidth);
            var role = result.Role.ToString().PadRight(RoleWidth);
            var snippet = FormatSnippet(result.Snippet, snippetWidth);

            output.WriteLine($"{score}  {chat}  {date}  {role}  {snippet}");
        }

        // Write footer with pagination info
        output.WriteLine();
        output.WriteLine($"Page {results.PageNumber}/{results.TotalPages} | Total: {results.TotalCount} results | Query time: {results.QueryTimeMs:F0}ms");
    }

    private static string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return new string(' ', maxLength);
        }

        if (value.Length <= maxLength)
        {
            return value.PadRight(maxLength);
        }

        return value.Substring(0, maxLength - 2) + "..";
    }

    private static string FormatSnippet(string snippet, int maxLength)
    {
        if (string.IsNullOrEmpty(snippet))
        {
            return string.Empty;
        }

        // Replace <mark> tags with ANSI color codes
        var formatted = snippet
            .Replace("<mark>", YellowBackground, StringComparison.Ordinal)
            .Replace("</mark>", ResetColor, StringComparison.Ordinal);

        // Calculate visible length (excluding ANSI codes)
        var visibleLength = snippet
            .Replace("<mark>", string.Empty, StringComparison.Ordinal)
            .Replace("</mark>", string.Empty, StringComparison.Ordinal)
            .Length;

        // Truncate if needed
        if (visibleLength > maxLength)
        {
            // This is a simplified truncation - in production would need to properly handle ANSI codes
            var plainText = snippet
                .Replace("<mark>", string.Empty, StringComparison.Ordinal)
                .Replace("</mark>", string.Empty, StringComparison.Ordinal);

            return plainText.Substring(0, maxLength - 2) + "..";
        }

        return formatted;
    }
}
