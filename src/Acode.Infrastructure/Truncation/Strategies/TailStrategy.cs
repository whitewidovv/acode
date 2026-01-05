namespace Acode.Infrastructure.Truncation.Strategies;

using System.Text;
using Acode.Application.Truncation;

/// <summary>
/// Truncation strategy that keeps only the tail (end) of content.
/// Best for logs and command output where recent content matters most.
/// </summary>
public sealed class TailStrategy : ITruncationStrategy
{
    /// <inheritdoc />
    public TruncationStrategy StrategyType => TruncationStrategy.Tail;

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

        // Find the last N lines
        var lines = content.Split('\n');
        var tailLines = Math.Min(limits.TailLines, lines.Length);

        // Calculate which lines to keep
        var keepFrom = lines.Length - tailLines;
        var omittedLineCount = keepFrom;

        // Build tail content
        var tailContent = new StringBuilder();
        for (var i = keepFrom; i < lines.Length; i++)
        {
            if (i > keepFrom)
            {
                tailContent.Append('\n');
            }

            tailContent.Append(lines[i]);
        }

        var tail = tailContent.ToString();

        // If tail is still too long, apply character truncation
        if (tail.Length > limits.InlineLimit)
        {
            var start = tail.Length - limits.InlineLimit + 60; // Reserve for marker
            tail = SafeSubstring(tail, start, limits.InlineLimit - 60);
        }

        // Calculate omitted characters
        var omittedChars = content.Length - tail.Length;

        // Build omission marker
        var marker = $"... [{omittedLineCount:N0} lines / {omittedChars:N0} chars omitted at beginning] ...\n";

        var truncatedContent = marker + tail;

        return new TruncationResult
        {
            Content = truncatedContent,
            Metadata = new TruncationMetadata
            {
                OriginalSize = content.Length,
                TruncatedSize = truncatedContent.Length,
                WasTruncated = true,
                StrategyUsed = TruncationStrategy.Tail,
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

        // Adjust start to not split a surrogate pair
        if (start > 0 && char.IsLowSurrogate(text[start]))
        {
            start--;
        }

        // Adjust end to not split a surrogate pair
        if (end < text.Length && char.IsHighSurrogate(text[end - 1]))
        {
            end--;
        }

        return text[start..end];
    }
}
