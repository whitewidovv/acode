namespace Acode.Application.Security;

/// <summary>
/// Result of secret redaction containing the redacted content and metadata.
/// </summary>
public sealed record RedactedContent
{
    /// <summary>
    /// Gets the content with secrets redacted.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets the number of secrets that were redacted.
    /// </summary>
    public required int RedactionCount { get; init; }

    /// <summary>
    /// Gets the types of secrets found (e.g., "api_key", "password", "token").
    /// </summary>
    public required IReadOnlyList<string> SecretTypesFound { get; init; }
}
