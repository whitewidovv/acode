namespace Acode.Domain.Models.Inference;

using System.Text.Json.Serialization;

/// <summary>
/// Represents an incremental update to a tool call during streaming responses.
/// </summary>
/// <remarks>
/// FR-004a-91 to FR-004a-100: Streaming tool call deltas support parallel tool calls.
/// The Index identifies which tool call this delta belongs to.
/// The first delta for a tool call includes Id and Name.
/// Subsequent deltas contain only ArgumentsDelta (partial JSON).
/// </remarks>
public sealed record ToolCallDelta
{
    /// <summary>
    /// Gets the zero-based index identifying which tool call this delta belongs to.
    /// </summary>
    /// <remarks>
    /// FR-004a-93, FR-004a-94: Index identifies which tool call is being built.
    /// Multiple tool calls can be streamed in parallel with different indices.
    /// </remarks>
    [JsonPropertyName("index")]
    public required int Index { get; init; }

    /// <summary>
    /// Gets the tool call ID (present only in the first delta for a tool call).
    /// </summary>
    /// <remarks>
    /// FR-004a-95, FR-004a-96: Id is present only in first delta for a tool call.
    /// </remarks>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; init; }

    /// <summary>
    /// Gets the tool name (present only in the first delta for a tool call).
    /// </summary>
    /// <remarks>
    /// FR-004a-97, FR-004a-98: Name is present only in first delta.
    /// </remarks>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; }

    /// <summary>
    /// Gets the partial JSON arguments string for this delta.
    /// </summary>
    /// <remarks>
    /// FR-004a-99, FR-004a-100: ArgumentsDelta is partial JSON string.
    /// Accumulate across deltas with the same Index to build complete arguments.
    /// </remarks>
    [JsonPropertyName("argumentsDelta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ArgumentsDelta { get; init; }
}
