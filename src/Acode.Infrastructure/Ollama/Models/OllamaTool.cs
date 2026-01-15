using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Ollama.Models;

/// <summary>
/// Tool definition for function calling.
/// </summary>
public sealed record OllamaTool
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaTool"/> class.
    /// </summary>
    /// <param name="type">The tool type.</param>
    /// <param name="function">The function definition.</param>
    public OllamaTool(string type, OllamaToolDefinition function)
    {
        this.Type = type;
        this.Function = function;
    }

    /// <summary>
    /// Gets the tool type (always "function").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; }

    /// <summary>
    /// Gets the function definition.
    /// </summary>
    [JsonPropertyName("function")]
    public OllamaToolDefinition Function { get; init; }
}
