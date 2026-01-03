namespace Acode.Application.Security;

/// <summary>
/// Service for detecting and redacting secrets from text content.
/// </summary>
public interface ISecretRedactor
{
    /// <summary>
    /// Redacts secrets from the given text content.
    /// </summary>
    /// <param name="content">The content to scan for secrets.</param>
    /// <returns>Redacted content with metadata about secrets found.</returns>
    RedactedContent Redact(string content);

    /// <summary>
    /// Redacts secrets from the given text content with optional context.
    /// </summary>
    /// <param name="content">The content to scan for secrets.</param>
    /// <param name="filePath">Optional file path for context-aware redaction.</param>
    /// <returns>Redacted content with metadata about secrets found.</returns>
    RedactedContent Redact(string content, string? filePath);
}
