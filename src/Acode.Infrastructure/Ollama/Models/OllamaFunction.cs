using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Ollama.Models;

/// <summary>
/// Function definition within a tool.
/// </summary>
public sealed record OllamaFunction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaFunction"/> class.
    /// </summary>
    /// <param name="name">The function name.</param>
    /// <param name="description">The function description.</param>
    /// <param name="parameters">The JSON Schema for parameters (optional).</param>
    /// <param name="strict">Whether to enforce strict validation (optional).</param>
    public OllamaFunction(
        string name,
        string description,
        object? parameters = null,
        bool? strict = null)
    {
        this.Name = name;
        this.Description = description;
        this.Parameters = parameters;
        this.Strict = strict;
    }

    /// <summary>
    /// Gets the function name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; }

    /// <summary>
    /// Gets the function description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; init; }

    /// <summary>
    /// Gets the JSON Schema for function parameters (optional).
    /// </summary>
    [JsonPropertyName("parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Parameters { get; init; }

    /// <summary>
    /// Gets a value indicating whether to enforce strict schema validation (optional).
    /// </summary>
    [JsonPropertyName("strict")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Strict { get; init; }
}
