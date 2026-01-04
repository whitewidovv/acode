namespace Acode.Infrastructure.Ollama.Exceptions;

using System;

/// <summary>
/// Base exception for all Ollama provider errors.
/// </summary>
/// <remarks>
/// FR-005-026: OllamaException is the base class for all Ollama-specific exceptions.
/// FR-005-027: All exceptions include error codes (ACODE-OLM-XXX format).
/// </remarks>
public class OllamaException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public OllamaException(string message)
        : base(message)
    {
        this.ErrorCode = "ACODE-OLM-000";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="innerException">Inner exception.</param>
    public OllamaException(string message, Exception innerException)
        : base(message, innerException)
    {
        this.ErrorCode = "ACODE-OLM-000";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="errorCode">Error code.</param>
    public OllamaException(string message, string errorCode)
        : base(message)
    {
        this.ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="errorCode">Error code.</param>
    /// <param name="innerException">Inner exception.</param>
    public OllamaException(string message, string errorCode, Exception innerException)
        : base(message, innerException)
    {
        this.ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    /// <remarks>
    /// FR-005-027: Error codes follow ACODE-OLM-XXX format.
    /// </remarks>
    public string ErrorCode { get; init; }
}
