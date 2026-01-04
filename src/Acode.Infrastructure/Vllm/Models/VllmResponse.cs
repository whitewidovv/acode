using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Vllm.Models;

/// <summary>
/// Represents a response from the vLLM /v1/chat/completions endpoint.
/// </summary>
public sealed class VllmResponse
{
    /// <summary>
    /// Gets or sets the response ID.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the object type (always "chat.completion").
    /// </summary>
    [JsonPropertyName("object")]
#pragma warning disable CA1720
    public string Object { get; set; } = "chat.completion";
#pragma warning restore CA1720

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    [JsonPropertyName("created")]
    public long Created { get; set; }

    /// <summary>
    /// Gets or sets the model used for generation.
    /// </summary>
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    /// <summary>
    /// Gets the response choices.
    /// </summary>
    [JsonPropertyName("choices")]
    public required List<VllmChoice> Choices { get; init; } = new();

    /// <summary>
    /// Gets or sets the token usage information.
    /// </summary>
    [JsonPropertyName("usage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VllmUsage? Usage { get; set; }
}
