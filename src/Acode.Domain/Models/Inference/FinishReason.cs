namespace Acode.Domain.Models.Inference;

using System.Text.Json.Serialization;

/// <summary>
/// Indicates why model generation stopped.
/// </summary>
/// <remarks>
/// FR-004b-019: FinishReason MUST be defined as an enum in the Domain layer.
/// FR-004b-020 to FR-004b-025: Values for different termination causes.
/// FR-004b-026: MUST serialize to lowercase snake_case strings in JSON.
/// FR-004b-027: MUST deserialize case-insensitively.
/// FR-004b-028, FR-004b-029: Maps from Ollama "done_reason" and vLLM "finish_reason".
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<FinishReason>))]
public enum FinishReason
{
    /// <summary>
    /// Normal completion - model finished generating naturally.
    /// </summary>
    Stop = 0,

    /// <summary>
    /// Max tokens reached - generation truncated due to length limit.
    /// </summary>
    Length = 1,

    /// <summary>
    /// Tool calls requested - generation stopped for tool execution.
    /// </summary>
    ToolCalls = 2,

    /// <summary>
    /// Content filter triggered - generation blocked by moderation.
    /// </summary>
    ContentFilter = 3,

    /// <summary>
    /// Error occurred - generation failed.
    /// </summary>
    Error = 4,

    /// <summary>
    /// Request cancelled - generation was cancelled by user.
    /// </summary>
    Cancelled = 5,
}
