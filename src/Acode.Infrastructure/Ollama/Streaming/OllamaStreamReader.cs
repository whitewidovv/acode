using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Acode.Infrastructure.Ollama.Models;

namespace Acode.Infrastructure.Ollama.Streaming;

/// <summary>
/// Reads NDJSON streaming responses from Ollama.
/// </summary>
/// <remarks>
/// FR-068 to FR-078 from Task 005.a.
/// Parses newline-delimited JSON chunks incrementally without buffering entire response.
/// </remarks>
public static class OllamaStreamReader
{
    /// <summary>
    /// Reads NDJSON chunks from a stream asynchronously.
    /// </summary>
    /// <param name="stream">The response stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of Ollama stream chunks.</returns>
    /// <remarks>
    /// FR-068: Reads NDJSON format (one JSON object per line).
    /// FR-071: Yields chunks via IAsyncEnumerable.
    /// FR-075: Propagates cancellation.
    /// </remarks>
    public static async IAsyncEnumerable<OllamaStreamChunk> ReadAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);

        // FR-069: Handle lines split across reads (StreamReader handles this)
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

            // FR-073: Handle empty lines gracefully
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // FR-070: Parse each line as OllamaStreamChunk
            OllamaStreamChunk? chunk;
            try
            {
                chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(line, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });
            }
            catch (JsonException)
            {
                // Skip malformed JSON lines
                continue;
            }

            if (chunk is null)
            {
                continue;
            }

            yield return chunk;

            // FR-072: Detect final chunk (done: true)
            if (chunk.Done)
            {
                yield break;
            }
        }

        // FR-076, FR-077, FR-078: Stream disposal handled by 'using' statement and 'leaveOpen: false'
    }
}
