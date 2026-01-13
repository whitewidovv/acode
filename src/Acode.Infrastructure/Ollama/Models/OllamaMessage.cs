using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Ollama.Models;

/// <summary>
/// A single message in the conversation.
/// </summary>
public sealed record OllamaMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaMessage"/> class.
    /// </summary>
    /// <param name="role">The message role.</param>
    /// <param name="content">The message content (optional).</param>
    /// <param name="toolCalls">The tool calls (optional).</param>
    /// <param name="toolCallId">The tool call ID (optional).</param>
    public OllamaMessage(
        string role,
        string? content = null,
        OllamaToolCallResponse[]? toolCalls = null,
        string? toolCallId = null)
    {
        this.Role = role;
        this.Content = content;
        this.ToolCalls = toolCalls;
        this.ToolCallId = toolCallId;
    }

    /// <summary>
    /// Gets the message role: "system", "user", "assistant", or "tool".
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; init; }

    /// <summary>
    /// Gets the message content (optional for tool calls).
    /// </summary>
    [JsonPropertyName("content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; init; }

    /// <summary>
    /// Gets the tool calls in assistant messages (optional).
    /// </summary>
    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OllamaToolCallResponse[]? ToolCalls { get; init; }

    /// <summary>
    /// Gets the tool call ID in tool result messages (optional).
    /// </summary>
    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; init; }
}
