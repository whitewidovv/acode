namespace Acode.Domain.Models.Inference;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Incremental update containing partial content or tool call fragments.
/// </summary>
/// <remarks>
/// FR-004b-056: ResponseDelta MUST be defined as an immutable record type.
/// FR-004b-057 to FR-004b-063: Properties, validation, streaming support.
/// </remarks>
public sealed record ResponseDelta
{
    public ResponseDelta(
        int index,
        string? contentDelta = null,
        string? toolCallDelta = null,
        FinishReason? finishReason = null,
        UsageInfo? usage = null)
    {
        // FR-004b-063: Validate at least one delta or complete
        if (contentDelta is null && toolCallDelta is null && finishReason is null)
        {
            throw new ArgumentException("ResponseDelta must have at least ContentDelta, ToolCallDelta, or FinishReason (final delta).");
        }

        this.Index = index;
        this.ContentDelta = contentDelta;
        this.ToolCallDelta = toolCallDelta;
        this.FinishReason = finishReason;
        this.Usage = usage;
    }

    /// <summary>
    /// Gets the position in the stream.
    /// </summary>
    /// <remarks>
    /// FR-004b-057: ResponseDelta MUST include Index property (int, position in stream).
    /// </remarks>
    [JsonPropertyName("index")]
    public int Index { get; init; }

    /// <summary>
    /// Gets the partial content string.
    /// </summary>
    /// <remarks>
    /// FR-004b-058: ResponseDelta MUST include optional ContentDelta property (string?).
    /// </remarks>
    [JsonPropertyName("contentDelta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContentDelta { get; init; }

    /// <summary>
    /// Gets the partial tool call fragment.
    /// </summary>
    /// <remarks>
    /// FR-004b-059: ResponseDelta MUST include optional ToolCallDelta property (ToolCallDelta? from 004.a).
    /// Note: Simplified to string for now - full ToolCallDelta type would be more complex.
    /// </remarks>
    [JsonPropertyName("toolCallDelta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallDelta { get; init; }

    /// <summary>
    /// Gets the finish reason (present only on final delta).
    /// </summary>
    /// <remarks>
    /// FR-004b-060: ResponseDelta MUST include optional FinishReason property (present only on final delta).
    /// </remarks>
    [JsonPropertyName("finishReason")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FinishReason? FinishReason { get; init; }

    /// <summary>
    /// Gets the usage information (present only on final delta).
    /// </summary>
    /// <remarks>
    /// FR-004b-061: ResponseDelta MUST include optional Usage property (present only on final delta).
    /// </remarks>
    [JsonPropertyName("usage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UsageInfo? Usage { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is the final delta.
    /// </summary>
    /// <remarks>
    /// FR-004b-062: ResponseDelta MUST provide bool IsComplete property (FinishReason != null).
    /// </remarks>
    [JsonIgnore]
    public bool IsComplete => this.FinishReason is not null;
}
