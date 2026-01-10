namespace Acode.Domain.Search;

/// <summary>
/// Exception thrown when a search operation fails with a specific error code and remediation.
/// </summary>
public sealed class SearchException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchException"/> class.
    /// </summary>
    /// <param name="errorCode">The error code (e.g., ACODE-SRCH-001).</param>
    /// <param name="message">The error message describing what went wrong.</param>
    /// <param name="remediation">Guidance on how to fix the error.</param>
    public SearchException(string errorCode, string message, string remediation)
        : base(message)
    {
        ErrorCode = errorCode;
        Remediation = remediation;
    }

    /// <summary>
    /// Gets the error code for this search failure.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Gets remediation guidance for resolving this error.
    /// </summary>
    public string Remediation { get; }
}
