namespace Acode.Domain.PromptPacks.Exceptions;

/// <summary>
/// Exception thrown when manifest parsing fails.
/// </summary>
public sealed class ManifestParseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestParseException"/> class.
    /// </summary>
    /// <param name="errorCode">The error code identifying the parse failure.</param>
    /// <param name="message">The error message.</param>
    public ManifestParseException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestParseException"/> class.
    /// </summary>
    /// <param name="errorCode">The error code identifying the parse failure.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ManifestParseException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets the error code identifying the type of parse failure.
    /// </summary>
    public string ErrorCode { get; }
}
