namespace Acode.Infrastructure.Vllm.Exceptions;

/// <summary>
/// Exception thrown when a vLLM request times out.
/// </summary>
public sealed class VllmTimeoutException : VllmException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VllmTimeoutException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public VllmTimeoutException(string message)
        : base("ACODE-VLM-002", message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmTimeoutException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public VllmTimeoutException(string message, Exception innerException)
        : base("ACODE-VLM-002", message, innerException)
    {
    }

    /// <inheritdoc/>
    public override bool IsTransient => true;
}
