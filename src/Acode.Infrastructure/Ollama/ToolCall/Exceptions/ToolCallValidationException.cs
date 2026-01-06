namespace Acode.Infrastructure.Ollama.ToolCall.Exceptions;

/// <summary>
/// Exception thrown when tool call arguments fail schema validation.
/// </summary>
[Serializable]
public sealed class ToolCallValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCallValidationException"/> class.
    /// </summary>
    public ToolCallValidationException()
        : base("Tool call arguments failed schema validation.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCallValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ToolCallValidationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCallValidationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ToolCallValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets or sets the error code (ACODE-TLP-XXX).
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Gets or sets the tool name.
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Gets or sets the JSON path to the invalid property.
    /// </summary>
    public string? JsonPath { get; set; }

    /// <summary>
    /// Gets or sets the expected type from schema.
    /// </summary>
    public string? ExpectedType { get; set; }

    /// <summary>
    /// Gets or sets the actual type received.
    /// </summary>
    public string? ActualType { get; set; }

    /// <summary>
    /// Gets or sets the list of validation errors.
    /// </summary>
    public IReadOnlyList<string>? ValidationErrors { get; set; }
}
