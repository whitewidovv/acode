namespace Acode.Infrastructure.Vllm.Exceptions;

/// <summary>
/// Exception thrown when unable to connect to vLLM server.
/// </summary>
public sealed class VllmConnectionException : VllmException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VllmConnectionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public VllmConnectionException(string message)
        : base("ACODE-VLM-001", message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmConnectionException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public VllmConnectionException(string message, Exception innerException)
        : base("ACODE-VLM-001", message, innerException)
    {
    }

    /// <inheritdoc/>
    public override bool IsTransient => true;
}
