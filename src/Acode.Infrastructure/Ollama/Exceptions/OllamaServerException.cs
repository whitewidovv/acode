namespace Acode.Infrastructure.Ollama.Exceptions;

using System;

/// <summary>
/// Exception thrown when Ollama server returns an error.
/// </summary>
/// <remarks>
/// FR-005-034: OllamaServerException for server errors (5xx).
/// FR-005-035: Error code ACODE-OLM-004.
/// FR-005-036: Includes HTTP status code.
/// </remarks>
public sealed class OllamaServerException : OllamaException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaServerException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="statusCode">HTTP status code.</param>
    public OllamaServerException(string message, int? statusCode = null)
        : base(message, "ACODE-OLM-004")
    {
        this.StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaServerException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="innerException">Inner exception.</param>
    /// <param name="statusCode">HTTP status code.</param>
    public OllamaServerException(string message, Exception innerException, int? statusCode = null)
        : base(message, "ACODE-OLM-004", innerException)
    {
        this.StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    /// <remarks>
    /// FR-005-036: StatusCode property for HTTP error code.
    /// </remarks>
    public int? StatusCode { get; }
}
