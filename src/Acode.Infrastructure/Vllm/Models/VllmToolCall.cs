using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Vllm.Models;

/// <summary>
/// Represents a tool call in a vLLM response.
/// </summary>
public sealed class VllmToolCall
{
    /// <summary>
    /// Gets or sets the tool call ID.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the type of tool call (always "function").
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Gets or sets the function details.
    /// </summary>
    [JsonPropertyName("function")]
    public required VllmFunction Function { get; set; }
}
