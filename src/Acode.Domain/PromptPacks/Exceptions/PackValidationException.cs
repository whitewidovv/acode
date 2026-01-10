namespace Acode.Domain.PromptPacks.Exceptions;

/// <summary>
/// Exception thrown when pack validation fails.
/// </summary>
public sealed class PackValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PackValidationException"/> class.
    /// </summary>
    /// <param name="errorCode">The error code identifying the validation failure.</param>
    /// <param name="message">The error message.</param>
    public PackValidationException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackValidationException"/> class.
    /// </summary>
    /// <param name="errorCode">The error code identifying the validation failure.</param>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public PackValidationException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets the error code identifying the type of validation failure.
    /// </summary>
    public string ErrorCode { get; }
}
