using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Vllm.Models;

/// <summary>
/// Represents function call details.
/// </summary>
public sealed class VllmFunction
{
    /// <summary>
    /// Gets or sets the function name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the function arguments as a JSON string.
    /// </summary>
    [JsonPropertyName("arguments")]
    public required string Arguments { get; set; }
}
