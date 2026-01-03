using System.Text.RegularExpressions;
using Acode.Application.Security;

namespace Acode.Infrastructure.Security;

/// <summary>
/// Redacts secrets from text using regular expression patterns.
/// </summary>
public sealed partial class RegexSecretRedactor : ISecretRedactor
{
    private static readonly Dictionary<string, Regex> SecretPatterns = new()
    {
        ["password"] = PasswordPattern(),
        ["api_key"] = ApiKeyPattern(),
        ["token"] = TokenPattern(),
        ["secret"] = SecretPattern(),
        ["private_key"] = PrivateKeyPattern(),
    };

    /// <inheritdoc/>
    public RedactedContent Redact(string content)
    {
        return Redact(content, null);
    }

    /// <inheritdoc/>
    public RedactedContent Redact(string content, string? filePath)
    {
        ArgumentNullException.ThrowIfNull(content);

        var redactedContent = content;
        var redactionCount = 0;
        var secretTypesFound = new HashSet<string>();

        foreach (var (secretType, pattern) in SecretPatterns)
        {
            var matches = pattern.Matches(redactedContent);
            if (matches.Count > 0)
            {
                redactionCount += matches.Count;
                secretTypesFound.Add(secretType);

                // Replace matches with [REDACTED]
                redactedContent = pattern.Replace(redactedContent, "[REDACTED]");
            }
        }

        return new RedactedContent
        {
            Content = redactedContent,
            RedactionCount = redactionCount,
            SecretTypesFound = secretTypesFound.ToList()
        };
    }

    // Password patterns
    [GeneratedRegex(@"(?i)(?:password|pwd|passwd)\s*[=:]\s*['""]?([^\s'""]+)", RegexOptions.Compiled)]
    private static partial Regex PasswordPattern();

    // API key patterns (minimum 10 chars to avoid false positives but catch most real keys)
    [GeneratedRegex(@"(?i)(?:api[_-]?key|apikey)\s*[=:]\s*['""]?([a-zA-Z0-9_-]{10,})", RegexOptions.Compiled)]
    private static partial Regex ApiKeyPattern();

    // Token patterns (JWT and similar, including partial JWTs in headers)
    [GeneratedRegex(@"(?i)(?:token|bearer|auth)(?:\s*[=:])?\s*['""]?(eyJ[a-zA-Z0-9_-]+(?:\.[a-zA-Z0-9_-]+)?(?:\.[a-zA-Z0-9_-]+)?|[a-zA-Z0-9_-]{32,})", RegexOptions.Compiled)]
    private static partial Regex TokenPattern();

    // Generic secret patterns
    [GeneratedRegex(@"(?i)(?:secret|credential)\s*[=:]\s*['""]?([^\s'""]+)", RegexOptions.Compiled)]
    private static partial Regex SecretPattern();

    // Private key patterns
    [GeneratedRegex(@"-----BEGIN\s+(?:RSA|EC|OPENSSH)?\s*PRIVATE\s+KEY-----[\s\S]+?-----END\s+(?:RSA|EC|OPENSSH)?\s*PRIVATE\s+KEY-----", RegexOptions.Compiled)]
    private static partial Regex PrivateKeyPattern();
}
