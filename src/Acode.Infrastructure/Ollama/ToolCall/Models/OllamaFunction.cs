namespace Acode.Infrastructure.Ollama.ToolCall.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the function details within a tool call.
/// </summary>
public sealed class OllamaFunction
{
    /// <summary>
    /// Gets or sets the name of the function/tool to invoke.
    /// Must match a registered tool in the schema registry.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON string containing the function arguments.
    /// May be malformed; JsonRepairer will attempt to fix common errors.
    /// </summary>
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = "{}";
}
