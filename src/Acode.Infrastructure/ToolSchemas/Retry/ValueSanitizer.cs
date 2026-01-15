using System.Text.RegularExpressions;

namespace Acode.Infrastructure.ToolSchemas.Retry;

/// <summary>
/// Sanitizes values to prevent secret leakage and manage output size.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3678-3806.
/// Detects and redacts JWT tokens, API keys, passwords, and other sensitive data.
/// </remarks>
public sealed partial class ValueSanitizer
{
    // Pre-compiled regex patterns for performance
    private static readonly Regex JwtPattern = JwtRegex();
    private static readonly Regex OpenAiKeyPattern = OpenAiKeyRegex();
    private static readonly Regex AwsKeyPattern = AwsKeyRegex();
    private static readonly Regex LongAlphanumericPattern = LongAlphanumericRegex();

    // Sensitive field names (case-insensitive matching)
    private static readonly HashSet<string> SensitiveFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "passwd",
        "pass",
        "pwd",
        "secret",
        "credentials",
        "api_key",
        "apiKey",
        "apikey",
        "access_key",
        "accessKey",
        "token",
        "auth_token",
        "authToken",
        "bearer",
        "jwt",
    };

    private readonly int maxPreviewLength;
    private readonly bool redactSecrets;
    private readonly bool relativizePaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValueSanitizer"/> class.
    /// </summary>
    /// <param name="maxPreviewLength">Maximum length of value preview.</param>
    /// <param name="redactSecrets">Whether to redact detected secrets.</param>
    /// <param name="relativizePaths">Whether to relativize absolute paths.</param>
    public ValueSanitizer(int maxPreviewLength, bool redactSecrets, bool relativizePaths)
    {
        this.maxPreviewLength = maxPreviewLength;
        this.redactSecrets = redactSecrets;
        this.relativizePaths = relativizePaths;
    }

    /// <summary>
    /// Sanitizes a value for safe display in error messages.
    /// </summary>
    /// <param name="value">The value to sanitize.</param>
    /// <param name="fieldPath">The JSON Pointer path to the field.</param>
    /// <returns>Sanitized value safe for display.</returns>
    public string Sanitize(string? value, string fieldPath)
    {
        ArgumentNullException.ThrowIfNull(fieldPath);

        if (value is null)
        {
            return "null";
        }

        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var result = value;

        if (this.redactSecrets)
        {
            // Check field name for sensitive data
            var fieldName = ExtractFieldName(fieldPath);
            if (IsSensitiveFieldName(fieldName))
            {
                return "[REDACTED: SENSITIVE_FIELD]";
            }

            // Check value patterns for secrets
            var redactedValue = RedactSecretPatterns(result);
            if (redactedValue != result)
            {
                return redactedValue;
            }
        }

        if (this.relativizePaths)
        {
            result = RelativizePath(result);
        }

        if (result.Length > this.maxPreviewLength)
        {
            result = TruncateWithSmartElision(result, this.maxPreviewLength);
        }

        return result;
    }

    private static string ExtractFieldName(string fieldPath)
    {
        // Extract the last segment from JSON Pointer path
        var lastSlash = fieldPath.LastIndexOf('/');
        return lastSlash >= 0 ? fieldPath[(lastSlash + 1)..] : fieldPath;
    }

    private static bool IsSensitiveFieldName(string fieldName)
    {
        return SensitiveFieldNames.Contains(fieldName);
    }

    private static string RedactSecretPatterns(string value)
    {
        // Check for JWT tokens
        if (JwtPattern.IsMatch(value))
        {
            return "[REDACTED: JWT]";
        }

        // Check for OpenAI API keys
        if (OpenAiKeyPattern.IsMatch(value))
        {
            return "[REDACTED: API_KEY]";
        }

        // Check for AWS Access Keys
        if (AwsKeyPattern.IsMatch(value))
        {
            return "[REDACTED: AWS_KEY]";
        }

        // Check for generic long alphanumeric (potential secrets)
        if (LongAlphanumericPattern.IsMatch(value) && value.Length >= 32)
        {
            return "[REDACTED: POTENTIAL_SECRET]";
        }

        return value;
    }

    private static string RelativizePath(string value)
    {
        // Handle Unix-style absolute paths
        if (value.StartsWith('/') && value.Contains('/', StringComparison.Ordinal))
        {
            var parts = value.Split('/');
            if (parts.Length > 3)
            {
                // Keep only the last few segments
                return "./" + string.Join("/", parts[^3..]);
            }
        }

        // Handle Windows-style absolute paths
        if (value.Length > 2 && char.IsLetter(value[0]) && value[1] == ':')
        {
            var parts = value.Split('\\', '/');
            if (parts.Length > 3)
            {
                return ".\\" + string.Join("\\", parts[^3..]);
            }
        }

        // Handle UNC paths
        if (value.StartsWith("\\\\", StringComparison.Ordinal))
        {
            var parts = value.Split('\\');
            if (parts.Length > 4)
            {
                return ".\\" + string.Join("\\", parts[^3..]);
            }
        }

        return value;
    }

    private static string TruncateWithSmartElision(string value, int maxLength)
    {
        if (value.Length <= maxLength)
        {
            return value;
        }

        // Calculate how much we can show from each end
        var ellipsis = "...";
        var availableLength = maxLength - ellipsis.Length;
        var prefixLength = availableLength / 2;
        var suffixLength = availableLength - prefixLength;

        var prefix = value[..prefixLength];
        var suffix = value[^suffixLength..];

        return prefix + ellipsis + suffix;
    }

    [GeneratedRegex(@"^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$", RegexOptions.Compiled)]
    private static partial Regex JwtRegex();

    [GeneratedRegex(@"^sk-[A-Za-z0-9]{32,}$", RegexOptions.Compiled)]
    private static partial Regex OpenAiKeyRegex();

    [GeneratedRegex(@"^AKIA[A-Z0-9]{16}$", RegexOptions.Compiled)]
    private static partial Regex AwsKeyRegex();

    [GeneratedRegex(@"^[A-Za-z0-9]{32,64}$", RegexOptions.Compiled)]
    private static partial Regex LongAlphanumericRegex();
}
