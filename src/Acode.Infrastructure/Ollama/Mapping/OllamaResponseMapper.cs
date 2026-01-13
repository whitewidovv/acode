using Acode.Domain.Models.Inference;
using Acode.Infrastructure.Ollama.Models;
using Acode.Infrastructure.Ollama.ToolCall;

namespace Acode.Infrastructure.Ollama.Mapping;

/// <summary>
/// Maps Ollama's response format to Acode's ChatResponse.
/// </summary>
public static class OllamaResponseMapper
{
    private static readonly ToolCallParser DefaultParser = new ToolCallParser();

    /// <summary>
    /// Maps an OllamaResponse to a ChatResponse.
    /// </summary>
    /// <param name="ollamaResponse">The Ollama response.</param>
    /// <param name="parser">Optional tool call parser (uses default if null).</param>
    /// <returns>A ChatResponse.</returns>
    public static ChatResponse Map(OllamaResponse ollamaResponse, ToolCallParser? parser = null)
    {
        ArgumentNullException.ThrowIfNull(ollamaResponse);

        // FR-053: Parse tool calls if present
        var toolCalls = ParseToolCalls(ollamaResponse.Message, parser ?? DefaultParser);

        // FR-053: Map message content to ChatMessage with tool calls
        var message = MapMessage(ollamaResponse.Message, toolCalls);

        // FR-054: Map done_reason to FinishReason (prefer ToolCalls if present)
        var finishReason = toolCalls != null && toolCalls.Count > 0
            ? FinishReason.ToolCalls
            : MapFinishReason(ollamaResponse.DoneReason);

        // FR-058: Calculate UsageInfo from token counts
        var usage = new UsageInfo(
            PromptTokens: ollamaResponse.PromptEvalCount ?? 0,
            CompletionTokens: ollamaResponse.EvalCount ?? 0);

        // FR-059: Calculate ResponseMetadata from timing
        var duration = ollamaResponse.TotalDuration.HasValue
            ? TimeSpan.FromMilliseconds(ollamaResponse.TotalDuration.Value / 1_000_000.0)
            : TimeSpan.Zero;

        var metadata = new ResponseMetadata(
            ProviderId: "ollama",
            ModelId: ollamaResponse.Model,
            RequestDuration: duration);

        // Parse created timestamp
        var created = DateTimeOffset.TryParse(
            ollamaResponse.CreatedAt,
            null,
            System.Globalization.DateTimeStyles.AssumeUniversal,
            out var parsedDate)
            ? parsedDate
            : DateTimeOffset.UtcNow;

        return new ChatResponse(
            Id: Guid.NewGuid().ToString(), // Generate unique ID
            Message: message,
            FinishReason: finishReason,
            Usage: usage,
            Metadata: metadata,
            Created: created,
            Model: ollamaResponse.Model);
    }

    /// <summary>
    /// Parses tool calls from Ollama message using ToolCallParser.
    /// </summary>
    /// <param name="ollamaMessage">The Ollama message.</param>
    /// <param name="parser">The tool call parser.</param>
    /// <returns>Parsed tool calls, or null if none or all failed.</returns>
    private static IReadOnlyList<Domain.Models.Inference.ToolCall>? ParseToolCalls(
        OllamaMessage ollamaMessage,
        ToolCallParser parser)
    {
        // No tool calls present
        if (ollamaMessage.ToolCalls == null || ollamaMessage.ToolCalls.Length == 0)
        {
            return null;
        }

        // Convert from Ollama.Models format to Ollama.ToolCall.Models format
        var toolCallsForParser = ConvertToolCalls(ollamaMessage.ToolCalls);

        // Parse tool calls with automatic JSON repair
        var parseResult = parser.Parse(toolCallsForParser);

        // Return parsed calls if any succeeded (even if some failed)
        // This supports FR-055: multiple simultaneous tool calls
        if (parseResult.ToolCalls.Count > 0)
        {
            return parseResult.ToolCalls;
        }

        // All parsing failed - return null
        return null;
    }

    /// <summary>
    /// Converts tool calls from Ollama.Models format to Ollama.ToolCall.Models format.
    /// </summary>
    /// <remarks>
    /// TODO Gap #13: Consolidate duplicate OllamaToolCall types.
    /// There are two incompatible OllamaToolCall types in the codebase:
    /// - Ollama.Models.OllamaToolCall (used in responses, has Description/Parameters)
    /// - Ollama.ToolCall.Models.OllamaToolCall (used by parser, has Arguments)
    /// This converter bridges the gap until types are consolidated.
    /// </remarks>
    private static ToolCall.Models.OllamaToolCall[] ConvertToolCalls(Models.OllamaToolCall[] toolCalls)
    {
        return toolCalls.Select(tc => new ToolCall.Models.OllamaToolCall
        {
            Id = tc.Id,
            Type = "function",
            Function = tc.Function != null
                ? new ToolCall.Models.OllamaFunction
                {
                    Name = tc.Function.Name,

                    // TODO: The Ollama.Models.OllamaFunction has Description/Parameters/Strict
                    // but should have Arguments for tool call responses. For now, serialize
                    // Parameters as JSON arguments. This needs proper fix in Gap #13.
                    Arguments = tc.Function.Parameters != null
                        ? System.Text.Json.JsonSerializer.Serialize(tc.Function.Parameters)
                        : "{}",
                }
                : null,
        }).ToArray();
    }

    /// <summary>
    /// Maps an OllamaMessage to a ChatMessage with tool calls.
    /// </summary>
    /// <param name="ollamaMessage">The Ollama message.</param>
    /// <param name="toolCalls">Parsed tool calls (nullable).</param>
    /// <returns>A ChatMessage.</returns>
    private static ChatMessage MapMessage(
        OllamaMessage ollamaMessage,
        IReadOnlyList<Domain.Models.Inference.ToolCall>? toolCalls)
    {
        var role = ollamaMessage.Role.ToLowerInvariant() switch
        {
            "system" => MessageRole.System,
            "user" => MessageRole.User,
            "assistant" => MessageRole.Assistant,
            "tool" => MessageRole.Tool,
            _ => MessageRole.Assistant, // Default to assistant
        };

        return role switch
        {
            MessageRole.System => ChatMessage.CreateSystem(ollamaMessage.Content ?? string.Empty),
            MessageRole.User => ChatMessage.CreateUser(ollamaMessage.Content ?? string.Empty),

            // FR-055: Support multiple tool calls in assistant messages
            MessageRole.Assistant => ChatMessage.CreateAssistant(
                content: ollamaMessage.Content,
                toolCalls: toolCalls),
            MessageRole.Tool => ChatMessage.CreateToolResult(
                toolCallId: ollamaMessage.ToolCallId ?? string.Empty,
                result: ollamaMessage.Content ?? string.Empty),
            _ => ChatMessage.CreateAssistant(ollamaMessage.Content ?? string.Empty),
        };
    }

    /// <summary>
    /// Maps Ollama's done_reason to FinishReason.
    /// </summary>
    private static FinishReason MapFinishReason(string? doneReason)
    {
        return doneReason?.ToLowerInvariant() switch
        {
            "stop" => FinishReason.Stop,        // FR-055
            "length" => FinishReason.Length,    // FR-056
            "tool_calls" => FinishReason.ToolCalls, // FR-057
            _ => FinishReason.Stop,             // Default to Stop
        };
    }
}
