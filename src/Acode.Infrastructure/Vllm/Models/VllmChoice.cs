using System.Text.Json.Serialization;

namespace Acode.Infrastructure.Vllm.Models;

/// <summary>
/// Represents a response choice.
/// </summary>
public sealed class VllmChoice
{
    /// <summary>
    /// Gets or sets the choice index.
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    [JsonPropertyName("message")]
    public required VllmMessage Message { get; set; }

    /// <summary>
    /// Gets or sets the finish reason.
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}
