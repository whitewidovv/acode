using System.Runtime.CompilerServices;

namespace Acode.Infrastructure.Vllm.Client.Streaming;

/// <summary>
/// Reads Server-Sent Events (SSE) from a stream, handling vLLM's format.
/// </summary>
/// <remarks>
/// FR-041 to FR-049, AC-041 to AC-049: VllmSseReader implementation.
/// Parses SSE format: lines starting with "data: " prefix contain JSON payloads.
/// Comments starting with ":" are ignored.
/// Empty lines separate events.
/// </remarks>
public sealed class VllmSseReader
{
    /// <summary>
    /// Reads SSE events from a stream, yielding JSON data strings.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of JSON data strings (without "data: " prefix).</returns>
    /// <remarks>
    /// FR-041: Parse lines with "data: " prefix.
    /// FR-042: Ignore comment lines (starting with ":").
    /// FR-052: Handle [DONE] marker to end stream (yielded as final event).
    /// </remarks>
    public async IAsyncEnumerable<string> ReadEventsAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, leaveOpen: false);

        string? line;

        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // FR-042: Skip comment lines (starting with ":")
            if (line.StartsWith(":", StringComparison.Ordinal))
            {
                continue;
            }

            // FR-041: Parse lines with "data: " prefix
            if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                var data = line.Substring(6); // Remove "data: " prefix

                yield return data;
            }
        }
    }
}
