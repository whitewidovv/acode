namespace Acode.Infrastructure.Ollama.Exceptions;

using System;

/// <summary>
/// Exception thrown when request to Ollama server is invalid.
/// </summary>
/// <remarks>
/// FR-005-032: OllamaRequestException for invalid request errors.
/// FR-005-033: Error code ACODE-OLM-003.
/// </remarks>
public sealed class OllamaRequestException : OllamaException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaRequestException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public OllamaRequestException(string message)
        : base(message, "ACODE-OLM-003")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaRequestException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="innerException">Inner exception.</param>
    public OllamaRequestException(string message, Exception innerException)
        : base(message, "ACODE-OLM-003", innerException)
    {
    }
}
