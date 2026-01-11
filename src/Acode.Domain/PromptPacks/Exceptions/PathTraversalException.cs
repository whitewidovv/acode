namespace Acode.Domain.PromptPacks.Exceptions;

/// <summary>
/// Exception thrown when a path traversal attack is detected.
/// </summary>
public sealed class PathTraversalException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathTraversalException"/> class.
    /// </summary>
    /// <param name="errorCode">The error code identifying the security violation.</param>
    /// <param name="message">The error message.</param>
    public PathTraversalException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Gets the error code identifying the type of security violation.
    /// </summary>
    public string ErrorCode { get; }
}
