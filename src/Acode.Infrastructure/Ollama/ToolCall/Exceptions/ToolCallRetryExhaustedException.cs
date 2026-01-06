namespace Acode.Infrastructure.Ollama.ToolCall.Exceptions;

/// <summary>
/// Exception thrown when all retry attempts for tool call parsing have been exhausted.
/// </summary>
[Serializable]
public sealed class ToolCallRetryExhaustedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCallRetryExhaustedException"/> class.
    /// </summary>
    public ToolCallRetryExhaustedException()
        : base("All retry attempts for tool call parsing have been exhausted.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCallRetryExhaustedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ToolCallRetryExhaustedException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolCallRetryExhaustedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ToolCallRetryExhaustedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets or sets the number of attempts made.
    /// </summary>
    public int AttemptsMade { get; set; }

    /// <summary>
    /// Gets or sets the maximum allowed attempts.
    /// </summary>
    public int MaxAttempts { get; set; }

    /// <summary>
    /// Gets or sets the tool name.
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Gets or sets the last error message from the final attempt.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Gets or sets the correlation ID for tracking.
    /// </summary>
    public string? CorrelationId { get; set; }
}
