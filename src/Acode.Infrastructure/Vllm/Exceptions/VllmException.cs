namespace Acode.Infrastructure.Vllm.Exceptions;

/// <summary>
/// Base exception for all vLLM provider errors.
/// </summary>
public class VllmException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VllmException"/> class.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">The error message.</param>
    public VllmException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmException"/> class.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public VllmException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the error code for this exception.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets or sets the request ID associated with this error.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Gets the timestamp when this exception was created.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets a value indicating whether this error is transient and may succeed on retry.
    /// </summary>
    public virtual bool IsTransient => false;
}
