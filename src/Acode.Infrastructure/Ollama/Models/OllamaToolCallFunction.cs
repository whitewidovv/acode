using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Ollama.Models;

/// <summary>
/// Function details for a tool call in Ollama response.
/// Used when the model invokes a tool, not for defining available tools.
/// </summary>
public sealed record OllamaToolCallFunction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaToolCallFunction"/> class.
    /// </summary>
    /// <param name="name">The function name.</param>
    /// <param name="arguments">The JSON string containing function arguments.</param>
    public OllamaToolCallFunction(
        string name,
        string arguments)
    {
        this.Name = name;
        this.Arguments = arguments;
    }

    /// <summary>
    /// Gets the function name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; }

    /// <summary>
    /// Gets the JSON string containing function arguments.
    /// This is a serialized JSON object that will be parsed by ToolCallParser.
    /// </summary>
    [JsonPropertyName("arguments")]
    public string Arguments { get; init; }
}
