namespace Acode.Infrastructure.Vllm.Exceptions;

/// <summary>
/// Exception thrown when vLLM server returns a 5xx error.
/// </summary>
public sealed class VllmServerException : VllmException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VllmServerException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public VllmServerException(string message)
        : base("ACODE-VLM-005", message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmServerException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public VllmServerException(string message, Exception innerException)
        : base("ACODE-VLM-005", message, innerException)
    {
    }

    /// <inheritdoc/>
    public override bool IsTransient => true;
}
