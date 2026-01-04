namespace Acode.Infrastructure.Ollama.Exceptions;

using System;

/// <summary>
/// Exception thrown when parsing Ollama response fails.
/// </summary>
/// <remarks>
/// FR-005-037: OllamaParseException for JSON parsing errors.
/// FR-005-038: Error code ACODE-OLM-005.
/// FR-005-039: Includes invalid JSON snippet.
/// </remarks>
public sealed class OllamaParseException : OllamaException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaParseException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="invalidJson">Invalid JSON snippet.</param>
    public OllamaParseException(string message, string? invalidJson = null)
        : base(message, "ACODE-OLM-005")
    {
        this.InvalidJson = invalidJson;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaParseException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="innerException">Inner exception.</param>
    /// <param name="invalidJson">Invalid JSON snippet.</param>
    public OllamaParseException(string message, Exception innerException, string? invalidJson = null)
        : base(message, "ACODE-OLM-005", innerException)
    {
        this.InvalidJson = invalidJson;
    }

    /// <summary>
    /// Gets the invalid JSON snippet.
    /// </summary>
    /// <remarks>
    /// FR-005-039: InvalidJson property for debugging.
    /// </remarks>
    public string? InvalidJson { get; }
}
