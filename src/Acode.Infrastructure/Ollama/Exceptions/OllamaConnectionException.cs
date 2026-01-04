namespace Acode.Infrastructure.Ollama.Exceptions;

using System;

/// <summary>
/// Exception thrown when connection to Ollama server fails.
/// </summary>
/// <remarks>
/// FR-005-028: OllamaConnectionException for connection failures.
/// FR-005-029: Error code ACODE-OLM-001.
/// </remarks>
public sealed class OllamaConnectionException : OllamaException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaConnectionException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public OllamaConnectionException(string message)
        : base(message, "ACODE-OLM-001")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaConnectionException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="innerException">Inner exception.</param>
    public OllamaConnectionException(string message, Exception innerException)
        : base(message, "ACODE-OLM-001", innerException)
    {
    }
}
