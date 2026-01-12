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
    [property: JsonPropertyName("index")]
    int Index,
    [property: JsonPropertyName("id")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Id = null,
    [property: JsonPropertyName("name")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Name = null,
    [property: JsonPropertyName("argumentsDelta")]
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? ArgumentsDelta = null);
