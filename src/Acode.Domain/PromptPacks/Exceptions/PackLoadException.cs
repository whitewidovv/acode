namespace Acode.Domain.PromptPacks.Exceptions;

/// <summary>
/// Exception thrown when pack loading fails.
/// </summary>
public sealed class PackLoadException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PackLoadException"/> class.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="packPath">The pack path.</param>
    public PackLoadException(string errorCode, string message, string? packPath = null)
        : base(message)
    {
        ErrorCode = errorCode;
        PackPath = packPath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackLoadException"/> class.
    /// </summary>
    /// <param name="errorCode">The error code.</param>
    /// <param name="message">The error message.</param>
    /// <param name="packPath">The pack path.</param>
    /// <param name="innerException">The inner exception.</param>
    public PackLoadException(string errorCode, string message, string? packPath, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        PackPath = packPath;
    }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets the pack path.
    /// </summary>
    public string? PackPath { get; }
}
