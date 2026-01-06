namespace Acode.Infrastructure.Truncation.Strategies;

using System.Text.Json;
using Acode.Application.Truncation;

/// <summary>
/// Truncation strategy for structured data (JSON arrays).
/// Preserves first/last elements while maintaining valid JSON structure.
/// </summary>
public sealed class ElementStrategy : ITruncationStrategy
{
    /// <inheritdoc />
    public TruncationStrategy StrategyType => TruncationStrategy.Element;

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

        // Try to parse as JSON
        try
        {
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                return TruncateJsonArray(content, root, limits);
            }

            // Fall back to head+tail for non-array JSON
            return FallbackTruncation(content, limits);
        }
        catch (JsonException)
        {
            // Not valid JSON, fall back to head+tail
            return FallbackTruncation(content, limits);
        }
    }

    /// <summary>
    /// Truncates a JSON array keeping first and last elements.
    /// </summary>
    private static TruncationResult TruncateJsonArray(
        string content,
        JsonElement array,
        TruncationLimits limits)
    {
        var totalElements = array.GetArrayLength();
        var firstCount = limits.FirstElements;
        var lastCount = limits.LastElements;

        // If we can keep all elements, no truncation needed
        if (totalElements <= firstCount + lastCount)
        {
            return TruncationResult.NotTruncated(content);
        }

        var omittedCount = totalElements - firstCount - lastCount;

        // Build truncated array
        var elements = new List<JsonElement>();
        var index = 0;
        foreach (var element in array.EnumerateArray())
        {
            if (index < firstCount || index >= totalElements - lastCount)
            {
                elements.Add(element);
            }

            index++;
        }

        // Serialize with omission marker
        var options = new JsonSerializerOptions { WriteIndented = false };
        var truncatedJson = BuildTruncatedJson(elements, firstCount, omittedCount, options);

        return new TruncationResult
        {
            Content = truncatedJson,
            Metadata = new TruncationMetadata
            {
                OriginalSize = content.Length,
                TruncatedSize = truncatedJson.Length,
                WasTruncated = true,
                StrategyUsed = TruncationStrategy.Element,
                OmittedCharacters = content.Length - truncatedJson.Length,
                OmittedElements = omittedCount
            }
        };
    }

    /// <summary>
    /// Builds the truncated JSON array with omission marker as a string element.
    /// </summary>
    private static string BuildTruncatedJson(
        List<JsonElement> elements,
        int firstCount,
        int omittedCount,
        JsonSerializerOptions options)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });

        writer.WriteStartArray();

        for (var i = 0; i < elements.Count; i++)
        {
            // Insert omission marker after first elements
            if (i == firstCount && omittedCount > 0)
            {
                writer.WriteStringValue($"... {omittedCount:N0} items omitted ...");
            }

            elements[i].WriteTo(writer);
        }

        writer.WriteEndArray();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Falls back to head+tail truncation for non-array content.
    /// </summary>
    private static TruncationResult FallbackTruncation(string content, TruncationLimits limits)
    {
        var headTail = new HeadTailStrategy();
        return headTail.Truncate(content, limits);
    }
}
