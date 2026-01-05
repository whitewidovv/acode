namespace Acode.Infrastructure.Truncation.Strategies;

using System.Text;
using Acode.Application.Truncation;

/// <summary>
/// Truncation strategy that keeps both head and tail of content.
/// Best for code files where both imports and main function matter.
/// </summary>
public sealed class HeadTailStrategy : ITruncationStrategy
{
    /// <inheritdoc />
    public TruncationStrategy StrategyType => TruncationStrategy.HeadTail;

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

        // Calculate head and tail sizes
        var headSize = (int)(limits.InlineLimit * limits.HeadRatio);
        var tailSize = limits.InlineLimit - headSize;

        // Reserve space for omission marker (estimate ~50 chars)
        var markerReserve = 60;
        headSize = Math.Max(10, headSize - (markerReserve / 2));
        tailSize = Math.Max(10, tailSize - (markerReserve / 2));

        // Extract head and tail respecting UTF-8 boundaries
        var head = SafeSubstring(content, 0, headSize);
        var tail = SafeSubstring(content, content.Length - tailSize, tailSize);

        // Calculate omitted content stats
        var omittedChars = content.Length - head.Length - tail.Length;
        var omittedLines = CountLinesBetween(content, head.Length, content.Length - tail.Length);

        // Build omission marker
        var marker = $"\n... [{omittedLines:N0} lines / {omittedChars:N0} chars omitted] ...\n";

        // Compose truncated content
        var result = new StringBuilder(head.Length + marker.Length + tail.Length);
        result.Append(head);
        result.Append(marker);
        result.Append(tail);

        var truncatedContent = result.ToString();

        return new TruncationResult
        {
            Content = truncatedContent,
            Metadata = new TruncationMetadata
            {
                OriginalSize = content.Length,
                TruncatedSize = truncatedContent.Length,
                WasTruncated = true,
                StrategyUsed = TruncationStrategy.HeadTail,
                OmittedCharacters = omittedChars,
                OmittedLines = omittedLines
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

    /// <summary>
    /// Counts the number of newlines between two positions in the text.
    /// </summary>
    private static int CountLinesBetween(string text, int startPos, int endPos)
    {
        var count = 0;
        for (var i = startPos; i < endPos && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                count++;
            }
        }

        return count;
    }
}
