namespace Acode.Application.Providers.Exceptions;

using System;

/// <summary>
/// Exception thrown when no provider capable of handling a request is found.
/// </summary>
/// <remarks>
/// FR-105 to FR-108 from task-004c spec.
/// Gap #14 from task-004c completion checklist.
/// </remarks>
public sealed class NoCapableProviderException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoCapableProviderException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="reason">Optional reason why no capable provider was found.</param>
    public NoCapableProviderException(string message, string? reason = null)
        : base(message)
    {
        ErrorCode = "ACODE-PRV-004";
        Reason = reason;
    }

    /// <summary>
    /// Gets the error code for this exception.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets the reason why no capable provider was found.
    /// </summary>
    public string? Reason { get; }
}
