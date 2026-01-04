using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Ollama.Models;

/// <summary>
/// Tool call in an assistant message or streaming response.
/// </summary>
public sealed record OllamaToolCall
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaToolCall"/> class.
    /// </summary>
    /// <param name="id">The unique ID for this tool call (optional).</param>
    /// <param name="function">The function being called (optional).</param>
    public OllamaToolCall(
        string? id = null,
        OllamaFunction? function = null)
    {
        this.Id = id;
        this.Function = function;
    }

    /// <summary>
    /// Gets the unique ID for this tool call (optional, generated if missing).
    /// </summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; init; }

    /// <summary>
    /// Gets the function being called.
    /// </summary>
    [JsonPropertyName("function")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OllamaFunction? Function { get; init; }
}
