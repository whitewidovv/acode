namespace Acode.Infrastructure.Ollama.ToolCall.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a tool call in Ollama's response format.
/// Maps from Ollama's JSON structure to internal representation.
/// </summary>
public sealed class OllamaToolCall
{
    /// <summary>
    /// Gets or sets the unique identifier for this tool call.
    /// May be null in some Ollama responses; will be generated if missing.
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the type of tool call. Always "function" for Ollama.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    /// <summary>
    /// Gets or sets the function to be called.
    /// </summary>
    [JsonPropertyName("function")]
    public OllamaFunction? Function { get; set; }
}
