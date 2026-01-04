namespace Acode.Infrastructure.Ollama.Exceptions;

using System;

/// <summary>
/// Exception thrown when request to Ollama server times out.
/// </summary>
/// <remarks>
/// FR-005-030: OllamaTimeoutException for timeout errors.
/// FR-005-031: Error code ACODE-OLM-002.
/// </remarks>
public sealed class OllamaTimeoutException : OllamaException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaTimeoutException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public OllamaTimeoutException(string message)
        : base(message, "ACODE-OLM-002")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaTimeoutException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="innerException">Inner exception.</param>
    public OllamaTimeoutException(string message, Exception innerException)
        : base(message, "ACODE-OLM-002", innerException)
    {
    }
}
