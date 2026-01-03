namespace Acode.Domain.Models.Inference;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the role of a message in a conversation with a language model.
/// </summary>
/// <remarks>
/// FR-004a-01: System MUST define MessageRole enum.
/// FR-004a-02 to FR-004a-05: Must have System, User, Assistant, Tool values.
/// FR-004a-06: Values serialize to lowercase strings.
/// FR-004a-07: Support case-insensitive parsing.
/// FR-004a-09: Has explicit integer values.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter<MessageRole>))]
public enum MessageRole
{
    /// <summary>
    /// System message defining agent behavior and constraints.
    /// </summary>
    System = 0,

    /// <summary>
    /// User message (human input).
    /// </summary>
    User = 1,

    /// <summary>
    /// Assistant message (model output).
    /// </summary>
    Assistant = 2,

    /// <summary>
    /// Tool result message (output from tool execution).
    /// </summary>
    Tool = 3,
}
