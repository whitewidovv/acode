namespace Acode.Infrastructure.Ollama.ToolCall.Models;

/// <summary>
/// Error encountered while parsing a tool call.
/// </summary>
public sealed class ToolCallError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCallError"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The error code.</param>
    public ToolCallError(string message, string errorCode)
    {
        Message = message;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// Gets the error code for programmatic handling (ACODE-TLP-XXX).
    /// </summary>
    public string ErrorCode { get; init; }

    /// <summary>
    /// Gets the tool name if known.
    /// </summary>
    public string? ToolName { get; init; }

    /// <summary>
    /// Gets the position in JSON where error occurred.
    /// </summary>
    public int? ErrorPosition { get; init; }

    /// <summary>
    /// Gets the original malformed JSON arguments.
    /// </summary>
    public string? RawArguments { get; init; }
}
