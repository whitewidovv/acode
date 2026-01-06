namespace Acode.Domain.PromptPacks;

/// <summary>
/// Exception thrown when a path traversal attack is detected.
/// </summary>
/// <remarks>
/// Path traversal attempts (../, absolute paths, etc.) are security violations
/// and must be rejected to prevent unauthorized file access.
/// </remarks>
public sealed class PathTraversalException : ArgumentException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathTraversalException"/> class.
    /// </summary>
    /// <param name="attemptedPath">The path that triggered the security violation.</param>
    public PathTraversalException(string attemptedPath)
        : base($"Path traversal attempt detected: '{attemptedPath}'. Paths must be relative and not contain directory traversal sequences.")
    {
        AttemptedPath = attemptedPath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PathTraversalException"/> class with a custom message.
    /// </summary>
    /// <param name="attemptedPath">The path that triggered the security violation.</param>
    /// <param name="message">The custom error message.</param>
    public PathTraversalException(string attemptedPath, string message)
        : base(message)
    {
        AttemptedPath = attemptedPath;
    }

    /// <summary>
    /// Gets the path that triggered the path traversal detection.
    /// </summary>
    public string AttemptedPath { get; }
}
