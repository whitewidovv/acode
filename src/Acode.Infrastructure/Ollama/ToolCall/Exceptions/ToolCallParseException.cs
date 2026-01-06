namespace Acode.Infrastructure.Ollama.ToolCall.Exceptions;

/// <summary>
/// Exception thrown when tool call parsing fails.
/// </summary>
[Serializable]
public sealed class ToolCallParseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCallParseException"/> class.
    /// </summary>
    public ToolCallParseException()
        : base("Failed to parse tool call.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCallParseException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ToolCallParseException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCallParseException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ToolCallParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets or sets the error code (ACODE-TLP-XXX).
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the tool name if known.
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Gets or sets the position in JSON where the error occurred.
    /// </summary>
    public int? ErrorPosition { get; set; }

    /// <summary>
    /// Gets or sets the raw malformed JSON.
    /// </summary>
    public string? RawJson { get; set; }
}
