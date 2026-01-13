namespace Acode.Application.Providers.Exceptions;

using System;

/// <summary>
/// Exception thrown when provider registration fails.
/// </summary>
/// <remarks>
/// FR-109 to FR-112 from task-004c spec.
/// Gap #15 from task-004c completion checklist.
/// </remarks>
public sealed class ProviderRegistrationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderRegistrationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The error code.</param>
    public ProviderRegistrationException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode ?? throw new ArgumentNullException(nameof(errorCode));
    }

    /// <summary>
    /// Gets the error code for this exception.
    /// </summary>
    public string ErrorCode { get; }
}
