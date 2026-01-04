using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Vllm.Models;

/// <summary>
/// Represents delta content in a streaming chunk.
/// </summary>
public sealed class VllmDelta
{
    /// <summary>
    /// Gets or sets the delta content.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Gets the delta tool calls.
    /// </summary>
    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<VllmToolCall>? ToolCalls { get; init; }

    /// <summary>
    /// Gets or sets the role (present in first chunk).
    /// </summary>
    [JsonPropertyName("role")]
    public string? Role { get; set; }
}
