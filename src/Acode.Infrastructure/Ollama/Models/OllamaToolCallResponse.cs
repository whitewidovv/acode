using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Ollama.Models;

/// <summary>
/// Represents a tool call in Ollama chat response.
/// This is the response format when the model invokes a tool.
/// </summary>
public sealed record OllamaToolCallResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaToolCallResponse"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this tool call (optional).</param>
    /// <param name="function">The function details (name and arguments).</param>
    public OllamaToolCallResponse(
        string? id = null,
        OllamaToolCallFunction? function = null)
    {
        this.Id = id;
        this.Function = function;
    }

    /// <summary>
    /// Gets the unique identifier for this tool call.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; init; }

    /// <summary>
    /// Gets the function details (name and arguments).
    /// </summary>
    [JsonPropertyName("function")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OllamaToolCallFunction? Function { get; init; }
}
