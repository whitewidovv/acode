namespace Acode.Infrastructure.Audit;

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Redacts sensitive data from audit events.
/// SECURITY CRITICAL: MUST catch all sensitive patterns.
/// </summary>
public sealed partial class AuditRedactor
{
    private const string RedactedMarker = "[REDACTED]";

    /// <summary>
    /// Redacts sensitive patterns from a string.
    /// </summary>
    /// <param name="input">Input string that may contain sensitive data.</param>
    /// <returns>String with sensitive data replaced by [REDACTED].</returns>
    public string Redact(string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var result = input;

        // Apply all patterns
        result = RedactWithPattern(result, PasswordPattern());
        result = RedactWithPattern(result, TokenPattern());
        result = RedactWithPattern(result, ApiKeyPattern());
        result = RedactWithPattern(result, SecretPattern());
        result = RedactWithPattern(result, BearerPattern());
        result = RedactWithPattern(result, PemKeyPattern());

        return result;
    }

    /// <summary>
    /// Redacts sensitive data from a dictionary based on key names.
    /// </summary>
    /// <param name="data">Dictionary that may contain sensitive data.</param>
    /// <returns>New dictionary with sensitive values redacted.</returns>
    public IDictionary<string, object> RedactData(IDictionary<string, object> data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var redacted = new Dictionary<string, object>();

        foreach (var (key, value) in data)
        {
            if (IsSensitiveKey(key))
            {
                redacted[key] = RedactedMarker;
            }
            else if (value is string strValue)
            {
                redacted[key] = Redact(strValue);
            }
            else
            {
                redacted[key] = value;
            }
        }

        return redacted;
    }

    private static string RedactWithPattern(string input, Regex pattern)
    {
        return pattern.Replace(
            input,
            match =>
            {
                // Special handling for Bearer tokens (no delimiter)
                if (match.Value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ||
                    match.Value.StartsWith("bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return $"Bearer {RedactedMarker}";
                }

                // Special handling for PEM keys
                if (match.Value.Contains("BEGIN", StringComparison.OrdinalIgnoreCase))
                {
                    return RedactedMarker;
                }

                // Standard key=value or key: value pattern
                var separatorIndex = match.Value.IndexOfAny(new[] { ':', '=' });
                if (separatorIndex >= 0)
                {
                    var prefix = match.Value.Substring(0, separatorIndex);
                    var separator = match.Value[separatorIndex];
                    return $"{prefix}{separator}{RedactedMarker}";
                }

                // Fallback - just redact the whole match
                return RedactedMarker;
            });
    }

    private static bool IsSensitiveKey(string key)
    {
        var lower = key.ToLowerInvariant();
        return lower.Contains("password", StringComparison.Ordinal) ||
               lower.Contains("secret", StringComparison.Ordinal) ||
               lower.Contains("token", StringComparison.Ordinal) ||
               lower.Contains("api_key", StringComparison.Ordinal) ||
               lower.Contains("apikey", StringComparison.Ordinal) ||
               lower.Contains("credential", StringComparison.Ordinal);
    }

    [GeneratedRegex(@"password[""']?\s*[:=]\s*[""']?[^""'\s,}]+", RegexOptions.IgnoreCase)]
    private static partial Regex PasswordPattern();

    [GeneratedRegex(@"token[""']?\s*[:=]\s*[""']?[^""'\s,}]+", RegexOptions.IgnoreCase)]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"api[_-]?key[""']?\s*[:=]\s*[""']?[^""'\s,}]+", RegexOptions.IgnoreCase)]
    private static partial Regex ApiKeyPattern();

    [GeneratedRegex(@"[a-z_]*secret[a-z_]*[""']?\s*[:=]\s*[""']?[^""'\s,}]+", RegexOptions.IgnoreCase)]
    private static partial Regex SecretPattern();

    [GeneratedRegex(@"bearer\s+[a-zA-Z0-9\-._~+/]+=*", RegexOptions.IgnoreCase)]
    private static partial Regex BearerPattern();

    [GeneratedRegex(@"-----BEGIN\s+[A-Z\s]+-----", RegexOptions.IgnoreCase)]
    private static partial Regex PemKeyPattern();
}
