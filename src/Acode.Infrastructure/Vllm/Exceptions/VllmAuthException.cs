namespace Acode.Infrastructure.Vllm.Exceptions;

/// <summary>
/// Exception thrown when authentication with vLLM fails (HTTP 401).
/// </summary>
public sealed class VllmAuthException : VllmException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VllmAuthException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public VllmAuthException(string message)
        : base("ACODE-VLM-011", message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmAuthException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public VllmAuthException(string message, Exception innerException)
        : base("ACODE-VLM-011", message, innerException)
    {
    }

    /// <inheritdoc/>
    public override bool IsTransient => false;
}
