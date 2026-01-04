using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Vllm.Models;

/// <summary>
/// Represents a streaming chunk from vLLM.
/// </summary>
public sealed class VllmStreamChunk
{
    /// <summary>
    /// Gets or sets the chunk ID.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the object type (always "chat.completion.chunk").
    /// </summary>
    [JsonPropertyName("object")]
#pragma warning disable CA1720
    public string Object { get; set; } = "chat.completion.chunk";
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
    /// Gets the streaming choices.
    /// </summary>
    [JsonPropertyName("choices")]
    public required List<VllmStreamChoice> Choices { get; init; } = new();

    /// <summary>
    /// Gets or sets the token usage information (present in final chunk).
    /// </summary>
    [JsonPropertyName("usage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public VllmUsage? Usage { get; set; }
}
