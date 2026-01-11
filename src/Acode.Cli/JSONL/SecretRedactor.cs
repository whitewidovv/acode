using System.Text.RegularExpressions;

namespace Acode.Cli.JSONL;

/// <summary>
/// Redacts sensitive information from event content.
/// </summary>
/// <remarks>
/// Detects and masks API keys, passwords, tokens, and other secrets
/// before they are emitted in JSONL output.
/// </remarks>
public sealed partial class SecretRedactor
{
    private static readonly string[] SecretKeyPatterns =
    [
        "api_key",
        "apikey",
        "api-key",
        "secret",
        "password",
        "passwd",
        "pwd",
        "token",
        "auth",
        "bearer",
        "credential",
        "key",
    ];

    /// <summary>
    /// Redacts a value based on its type.
    /// </summary>
    /// <param name="value">The value to redact.</param>
    /// <param name="type">The type of secret (e.g., "api_key", "password").</param>
    /// <returns>Redacted string showing only last 4 characters.</returns>
    public string Redact(string value, string type)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "***";
        }

        if (value.Length <= 4)
        {
            return "***";
        }

        // Show last 4 characters for identification
        return $"***{value[^4..]}";
    }

    /// <summary>
    /// Determines if a key name suggests it contains a secret.
    /// </summary>
    /// <param name="key">The key name to check.</param>
    /// <returns>True if the key likely contains a secret.</returns>
    public bool IsSecret(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        var lowerKey = key.ToLowerInvariant();
        return SecretKeyPatterns.Any(pattern =>
            lowerKey.Contains(pattern, StringComparison.Ordinal)
        );
    }

    /// <summary>
    /// Redacts all secrets in a dictionary.
    /// </summary>
    /// <param name="data">Dictionary to process.</param>
    /// <returns>New dictionary with secrets redacted.</returns>
    public IReadOnlyDictionary<string, object> RedactDictionary(
        IReadOnlyDictionary<string, object> data
    )
    {
        ArgumentNullException.ThrowIfNull(data);

        var result = new Dictionary<string, object>();

        foreach (var kvp in data)
        {
            if (IsSecret(kvp.Key) && kvp.Value is string stringValue)
            {
                result[kvp.Key] = Redact(stringValue, kvp.Key);
            }
            else
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    /// <summary>
    /// Redacts common secret patterns in a string (like API keys in URLs).
    /// </summary>
    /// <param name="text">Text to process.</param>
    /// <returns>Text with secrets redacted.</returns>
    public string RedactString(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        // Redact API keys in URL query params
        text = ApiKeyInUrlRegex().Replace(text, "$1***");

        // Redact Bearer tokens
        text = BearerTokenRegex().Replace(text, "Bearer ***");

        return text;
    }

    [GeneratedRegex(@"(api[_-]?key=)[^&\s]+", RegexOptions.IgnoreCase)]
    private static partial Regex ApiKeyInUrlRegex();

    [GeneratedRegex(@"Bearer\s+[^\s]+", RegexOptions.IgnoreCase)]
    private static partial Regex BearerTokenRegex();
}
