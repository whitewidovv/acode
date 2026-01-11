// src/Acode.Infrastructure/Persistence/Conversation/Exceptions/ConnectionException.cs
namespace Acode.Infrastructure.Persistence.Conversation.Exceptions;

using System;

/// <summary>
/// Exception thrown when database connection or execution fails.
/// </summary>
public sealed class ConnectionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ConnectionException(string message)
        : base(message)
    {
        ErrorCode = "ACODE-CONV-DATA-008";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class with an operation name.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="operation">The operation that failed.</param>
    public ConnectionException(string message, string operation)
        : base(message)
    {
        ErrorCode = "ACODE-CONV-DATA-008";
        Operation = operation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class with an inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = "ACODE-CONV-DATA-008";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class with an operation and inner exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConnectionException(string message, string operation, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = "ACODE-CONV-DATA-008";
        Operation = operation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionException"/> class with a custom error code.
    /// </summary>
    /// <param name="errorCode">The error code for this exception.</param>
    /// <param name="message">The exception message.</param>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConnectionException(string errorCode, string message, string? operation, Exception? innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Operation = operation;
    }

    /// <summary>
    /// Gets the error code for this exception.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets the operation that failed.
    /// </summary>
    public string? Operation { get; }
}
