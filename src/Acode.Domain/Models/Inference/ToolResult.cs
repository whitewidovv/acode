namespace Acode.Domain.Models.Inference;

using System.Text.Json.Serialization;

/// <summary>
/// Represents the result of a tool execution.
/// </summary>
/// <remarks>
/// FR-004a-56: System MUST define ToolResult record.
/// FR-004a-57: ToolResult MUST be immutable.
/// FR-004a-58 to FR-004a-60: Must have ToolCallId, Result, IsError properties.
/// FR-004a-65: JSON serialization support.
/// FR-004a-67 to FR-004a-69: Factory methods for Success and Error.
/// </remarks>
[method: JsonConstructor]
public sealed record ToolResult(string ToolCallId, string Result, bool IsError = false)
{
    /// <summary>
    /// Gets the ID of the tool call that produced this result.
    /// </summary>
    /// <remarks>
    /// FR-004a-58, FR-004a-59: ToolCallId MUST match corresponding ToolCall.Id.
    /// </remarks>
    [JsonPropertyName("toolCallId")]
    public string ToolCallId { get; init; } = ValidateToolCallId(ToolCallId);

    /// <summary>
    /// Gets the result content (serialized output or error message).
    /// </summary>
    /// <remarks>
    /// FR-004a-60: Result MUST be string.
    /// FR-004a-61: Result is serialized output.
    /// FR-004a-62: Result MAY be empty string.
    /// </remarks>
    [JsonPropertyName("result")]
    public string Result { get; init; } = ValidateResult(Result);

    /// <summary>
    /// Gets a value indicating whether this result represents an error.
    /// </summary>
    /// <remarks>
    /// FR-004a-63: ToolResult MUST have IsError property.
    /// FR-004a-64: IsError MUST default to false.
    /// </remarks>
    [JsonPropertyName("isError")]
    public bool IsError { get; init; } = IsError;

    /// <summary>
    /// Creates a successful tool result.
    /// </summary>
    /// <param name="toolCallId">The ID of the tool call that was executed.</param>
    /// <param name="result">The result content from the tool execution.</param>
    /// <returns>A ToolResult representing success.</returns>
    /// <remarks>
    /// FR-004a-67: ToolResult MUST have factory: Success(id, result).
    /// </remarks>
    public static ToolResult Success(string toolCallId, string result)
    {
        return new ToolResult(toolCallId, result, false);
    }

    /// <summary>
    /// Creates an error tool result.
    /// </summary>
    /// <param name="toolCallId">The ID of the tool call that failed.</param>
    /// <param name="errorMessage">The error message describing what went wrong.</param>
    /// <returns>A ToolResult representing an error.</returns>
    /// <remarks>
    /// FR-004a-68: ToolResult MUST have factory: Error(id, message).
    /// FR-004a-69: Error factory MUST set IsError to true.
    /// </remarks>
    public static ToolResult Error(string toolCallId, string errorMessage)
    {
        return new ToolResult(toolCallId, errorMessage, true);
    }

    private static string ValidateToolCallId(string toolCallId)
    {
        // FR-004a-66: ToolResult MUST validate on construction
        if (string.IsNullOrWhiteSpace(toolCallId))
        {
            throw new ArgumentException("ToolResult ToolCallId must be non-empty.", nameof(ToolCallId));
        }

        return toolCallId;
    }

    private static string ValidateResult(string result)
    {
        // FR-004a-66: ToolResult MUST validate on construction
        // FR-004a-62: Result MAY be empty string (but not null)
        if (result is null)
        {
            throw new ArgumentException("ToolResult Result must not be null.", nameof(Result));
        }

        return result;
    }
}
