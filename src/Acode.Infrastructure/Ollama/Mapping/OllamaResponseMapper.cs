using Acode.Domain.Models.Inference;
using Acode.Infrastructure.Ollama.Models;

namespace Acode.Infrastructure.Ollama.Mapping;

/// <summary>
/// Maps Ollama's response format to Acode's ChatResponse.
/// </summary>
public static class OllamaResponseMapper
{
    /// <summary>
    /// Maps an OllamaResponse to a ChatResponse.
    /// </summary>
    /// <param name="ollamaResponse">The Ollama response.</param>
    /// <returns>A ChatResponse.</returns>
    public static ChatResponse Map(OllamaResponse ollamaResponse)
    {
        ArgumentNullException.ThrowIfNull(ollamaResponse);

        // FR-053: Map message content to ChatMessage
        var message = MapMessage(ollamaResponse.Message);

        // FR-054: Map done_reason to FinishReason
        var finishReason = MapFinishReason(ollamaResponse.DoneReason);

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
    /// Maps an OllamaMessage to a ChatMessage.
    /// </summary>
    private static ChatMessage MapMessage(OllamaMessage ollamaMessage)
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
            MessageRole.Assistant => ChatMessage.CreateAssistant(ollamaMessage.Content ?? string.Empty),
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
