namespace Acode.Infrastructure.Vllm.Exceptions;

/// <summary>
/// Interface for all vLLM exceptions.
/// </summary>
public interface IVllmException
{
    /// <summary>
    /// Gets the error code for this exception.
    /// </summary>
    string ErrorCode { get; }

    /// <summary>
    /// Gets or sets the request ID associated with this error.
    /// </summary>
    string? RequestId { get; set; }

    /// <summary>
    /// Gets the timestamp when this exception was created.
    /// </summary>
    DateTime Timestamp { get; }

    /// <summary>
    /// Gets a value indicating whether this error is transient and may succeed on retry.
    /// </summary>
    bool IsTransient { get; }
}
