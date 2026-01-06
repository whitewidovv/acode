namespace Acode.Domain.PromptPacks;

/// <summary>
/// Base exception for all prompt pack related errors.
/// </summary>
public class PackException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PackException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public PackException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public PackException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
