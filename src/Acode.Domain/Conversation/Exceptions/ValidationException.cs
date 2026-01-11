// src/Acode.Domain/Conversation/Exceptions/ValidationException.cs
namespace Acode.Domain.Conversation.Exceptions;

using System;

/// <summary>
/// Exception thrown when entity validation fails.
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="validationErrors">The validation errors.</param>
    public ValidationException(string message, params string[] validationErrors)
        : base(message)
    {
        ErrorCode = "ACODE-CONV-DATA-007";
        ValidationErrors = validationErrors ?? Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a custom error code.
    /// </summary>
    /// <param name="errorCode">The error code for this exception.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="validationErrors">The validation errors.</param>
    public ValidationException(string errorCode, string message, params string[] validationErrors)
        : base(message)
    {
        ErrorCode = errorCode;
        ValidationErrors = validationErrors ?? Array.Empty<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="validationErrors">The validation errors.</param>
    public ValidationException(string message, Exception innerException, params string[] validationErrors)
        : base(message, innerException)
    {
        ErrorCode = "ACODE-CONV-DATA-007";
        ValidationErrors = validationErrors ?? Array.Empty<string>();
    }

    /// <summary>
    /// Gets the error code for this exception.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public string[] ValidationErrors { get; }
}
