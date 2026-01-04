using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Vllm.Models;

/// <summary>
/// Represents a streaming choice.
/// </summary>
public sealed class VllmStreamChoice
{
    /// <summary>
    /// Gets or sets the choice index.
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the delta content.
    /// </summary>
    [JsonPropertyName("delta")]
    public required VllmDelta Delta { get; set; }

    /// <summary>
    /// Gets or sets the finish reason (present in final chunk).
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}
