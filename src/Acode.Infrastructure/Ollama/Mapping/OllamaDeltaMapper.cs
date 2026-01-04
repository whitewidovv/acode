using Acode.Domain.Models.Inference;
using Acode.Infrastructure.Ollama.Models;

namespace Acode.Infrastructure.Ollama.Mapping;

/// <summary>
/// Maps Ollama stream chunks to ResponseDelta.
/// </summary>
public static class OllamaDeltaMapper
{
    /// <summary>
    /// Maps an OllamaStreamChunk to a ResponseDelta.
    /// </summary>
    /// <param name="chunk">The Ollama stream chunk.</param>
    /// <param name="index">The index/position in the stream.</param>
    /// <returns>A ResponseDelta.</returns>
    public static ResponseDelta MapToDelta(OllamaStreamChunk chunk, int index)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        // FR-079: Extract content delta from chunk
        var contentDelta = chunk.Message?.Content;

        // FR-081: Detect final chunk (done: true)
        // FR-082 to FR-084: Map done_reason to FinishReason
        FinishReason? finishReason = null;
        UsageInfo? usage = null;

        if (chunk.Done)
        {
            finishReason = MapFinishReason(chunk.DoneReason);

            // FR-084: Calculate UsageInfo from token counts (final chunk only)
            if (chunk.PromptEvalCount.HasValue || chunk.EvalCount.HasValue)
            {
                usage = new UsageInfo(
                    PromptTokens: chunk.PromptEvalCount ?? 0,
                    CompletionTokens: chunk.EvalCount ?? 0);
            }
        }

        // FR-086: Create ResponseDelta
        // Note: For non-final chunks, we need contentDelta
        // For final chunks, we need either contentDelta or finishReason
        if (contentDelta is not null)
        {
            return new ResponseDelta(
                index: index,
                contentDelta: contentDelta,
                toolCallDelta: null,
                finishReason: finishReason,
                usage: usage);
        }
        else if (finishReason is not null)
        {
            // Final chunk with no content (just done marker)
            return new ResponseDelta(
                index: index,
                contentDelta: null,
                toolCallDelta: null,
                finishReason: finishReason,
                usage: usage);
        }
        else
        {
            // Shouldn't happen, but handle gracefully
            return new ResponseDelta(
                index: index,
                contentDelta: string.Empty,
                toolCallDelta: null,
                finishReason: null,
                usage: null);
        }
    }

    /// <summary>
    /// Maps Ollama's done_reason to FinishReason.
    /// </summary>
    private static FinishReason MapFinishReason(string? doneReason)
    {
        // FR-082, FR-083, FR-084, FR-085
        return doneReason?.ToLowerInvariant() switch
        {
            "stop" => FinishReason.Stop,
            "length" => FinishReason.Length,
            "tool_calls" => FinishReason.ToolCalls,
            _ => FinishReason.Stop, // Default to Stop
        };
    }
}
