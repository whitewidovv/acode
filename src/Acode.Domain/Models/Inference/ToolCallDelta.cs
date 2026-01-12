namespace Acode.Domain.Models.Inference;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a streaming update to a tool call during response generation.
/// </summary>
/// <remarks>
/// FR-004a-91: System MUST define ToolCallDelta record.
/// FR-004a-92 to FR-004a-100: Immutability, properties, streaming support.
/// </remarks>
[method: JsonConstructor]
public sealed record ToolCallDelta(
    int Index,
    string? Id = null,
    string? Name = null,
    string? ArgumentsDelta = null)
{
    /// <summary>
    /// Gets the index identifying which tool call is being built.
    /// </summary>
    /// <remarks>
    /// FR-004a-93: ToolCallDelta MUST have Index property.
    /// FR-004a-94: Index identifies which tool call is being built.
    /// </remarks>
    [JsonPropertyName("index")]
    public int Index { get; init; } = Index;

    /// <summary>
    /// Gets the tool call ID (present only in first delta).
    /// </summary>
    /// <remarks>
    /// FR-004a-95: ToolCallDelta MAY have Id property.
    /// FR-004a-96: Id is present only in first delta for a tool call.
    /// </remarks>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; init; } = Id;

    /// <summary>
    /// Gets the tool name (present only in first delta).
    /// </summary>
    /// <remarks>
    /// FR-004a-97: ToolCallDelta MAY have Name property.
    /// FR-004a-98: Name is present only in first delta.
    /// </remarks>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; init; } = Name;

    /// <summary>
    /// Gets the partial JSON arguments fragment.
    /// </summary>
    /// <remarks>
    /// FR-004a-99: ToolCallDelta MAY have ArgumentsDelta property.
    /// FR-004a-100: ArgumentsDelta is string (partial JSON).
    /// </remarks>
    [JsonPropertyName("argumentsDelta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ArgumentsDelta { get; init; } = ArgumentsDelta;
}
