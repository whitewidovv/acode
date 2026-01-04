namespace Acode.Infrastructure.Vllm.Exceptions;

/// <summary>
/// Exception thrown when a request to vLLM is invalid (HTTP 400).
/// </summary>
public sealed class VllmRequestException : VllmException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VllmRequestException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public VllmRequestException(string message)
        : base("ACODE-VLM-004", message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmRequestException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public VllmRequestException(string message, Exception innerException)
        : base("ACODE-VLM-004", message, innerException)
    {
    }

    /// <inheritdoc/>
    public override bool IsTransient => false;
}
