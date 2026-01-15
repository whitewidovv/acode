namespace Acode.Infrastructure.Vllm.Client.Authentication;

/// <summary>
/// Handles authentication for vLLM requests, including API key management and redaction.
/// </summary>
/// <remarks>
/// FR-086 through FR-090, AC-086 through AC-090: Manages API key configuration,
/// environment variable overrides, and secure redaction for logging.
/// </remarks>
public sealed class VllmAuthHandler
{
    private const string RedactedKey = "[REDACTED]";
    private readonly string? _apiKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="VllmAuthHandler"/> class.
    /// </summary>
    /// <param name="configApiKey">API key from configuration (optional).</param>
    /// <param name="environmentVariableName">Environment variable name to check for override (default: VLLM_API_KEY).</param>
    public VllmAuthHandler(string? configApiKey = null, string environmentVariableName = "VLLM_API_KEY")
    {
        // FR-086, AC-086: Read API key from environment (override)
        var envKey = Environment.GetEnvironmentVariable(environmentVariableName);

        // Environment variable overrides config
        _apiKey = !string.IsNullOrWhiteSpace(envKey) ? envKey : configApiKey;
    }

    /// <summary>
    /// Gets a value indicating whether an API key is configured.
    /// </summary>
    public bool HasApiKey => !string.IsNullOrWhiteSpace(_apiKey);

    /// <summary>
    /// Gets the Authorization header value if API key is configured.
    /// </summary>
    /// <returns>Authorization header value ("Bearer {key}") or null if no key.</returns>
    public string? GetAuthorizationHeaderValue()
    {
        // FR-090, AC-090: Work without API key when not configured
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return null;
        }

        // FR-087, AC-087: Format as "Bearer {key}"
        return $"Bearer {_apiKey}";
    }

    /// <summary>
    /// Gets a redacted version of the API key for logging/error messages.
    /// </summary>
    /// <returns>Redacted key string.</returns>
    public string GetRedactedKey()
    {
        // FR-088, FR-089, AC-088, AC-089: NEVER log actual key, always redact
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return "(no key)";
        }

        return RedactedKey;
    }
}
