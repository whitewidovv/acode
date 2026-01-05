namespace Acode.Infrastructure.Truncation.Strategies;

using System.Text;
using Acode.Application.Truncation;

/// <summary>
/// Truncation strategy that keeps only the head (beginning) of content.
/// Best for documentation files where key info is at the top.
/// </summary>
public sealed class HeadStrategy : ITruncationStrategy
{
    /// <inheritdoc />
    public TruncationStrategy StrategyType => TruncationStrategy.Head;

    /// <inheritdoc />
    public TruncationResult Truncate(string content, TruncationLimits limits)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(limits);

        // Content under limit - no truncation needed
        if (content.Length <= limits.InlineLimit)
        {
            return TruncationResult.NotTruncated(content);
        }

        // Find the first N lines
        var lines = content.Split('\n');
        var headLines = Math.Min(limits.HeadLines, lines.Length);

        // Build head content
        var headContent = new StringBuilder();
        for (var i = 0; i < headLines; i++)
        {
            if (i > 0)
            {
                headContent.Append('\n');
            }

            headContent.Append(lines[i]);
        }

        var head = headContent.ToString();

        // If head is still too long, apply character truncation
        if (head.Length > limits.InlineLimit)
        {
            head = SafeSubstring(head, 0, limits.InlineLimit - 60);
        }

        // Calculate omitted content stats
        var omittedChars = content.Length - head.Length;
        var omittedLineCount = lines.Length - headLines;

        // Build omission marker
        var marker = $"\n... [{omittedLineCount:N0} lines / {omittedChars:N0} chars omitted at end] ...";

        var truncatedContent = head + marker;

        return new TruncationResult
        {
            Content = truncatedContent,
            Metadata = new TruncationMetadata
            {
                OriginalSize = content.Length,
                TruncatedSize = truncatedContent.Length,
                WasTruncated = true,
                StrategyUsed = TruncationStrategy.Head,
                OmittedCharacters = omittedChars,
                OmittedLines = omittedLineCount
            }
        };
    }

    /// <summary>
    /// Safely extracts a substring respecting UTF-8 character boundaries.
    /// </summary>
    private static string SafeSubstring(string text, int start, int length)
    {
        if (start < 0)
        {
            start = 0;
        }

        if (start >= text.Length)
        {
            return string.Empty;
        }

        var end = Math.Min(start + length, text.Length);

        // Adjust end to not split a surrogate pair
        if (end < text.Length && char.IsHighSurrogate(text[end - 1]))
        {
            end--;
        }

        return text[start..end];
    }
}
