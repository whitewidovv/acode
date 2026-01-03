namespace Acode.Domain.Configuration;

/// <summary>
/// Default values for Acode configuration.
/// These defaults are used when values are not explicitly specified in .agent/config.yml.
/// </summary>
/// <remarks>
/// All defaults MUST match the JSON Schema specification in data/config-schema.json.
/// Per Task 002.b FR-002b-91 through FR-002b-105.
/// </remarks>
public static class ConfigDefaults
{
    /// <summary>
    /// Default schema version.
    /// Per FR-002b-94.
    /// </summary>
    public const string SchemaVersion = "1.0.0";

    /// <summary>
    /// Default operating mode.
    /// Per FR-002b-95 and HC-07 (fail-safe to LocalOnly).
    /// </summary>
    public const string DefaultMode = "local-only";

    /// <summary>
    /// Default allow_burst setting.
    /// Per FR-002b-96.
    /// </summary>
    public const bool AllowBurst = true;

    /// <summary>
    /// Default airgapped_lock setting.
    /// Per FR-002b-97.
    /// </summary>
    public const bool AirgappedLock = false;

    /// <summary>
    /// Default LLM provider.
    /// Per FR-002b-98.
    /// </summary>
    public const string DefaultProvider = "ollama";

    /// <summary>
    /// Default model name.
    /// Per FR-002b-99.
    /// </summary>
    public const string DefaultModel = "codellama:7b";

    /// <summary>
    /// Default model endpoint.
    /// Per FR-002b-100.
    /// </summary>
    public const string DefaultEndpoint = "http://localhost:11434";

    /// <summary>
    /// Default temperature parameter.
    /// Per FR-002b-101.
    /// </summary>
    public const double DefaultTemperature = 0.7;

    /// <summary>
    /// Default max_tokens parameter.
    /// Per FR-002b-102.
    /// </summary>
    public const int DefaultMaxTokens = 4096;

    /// <summary>
    /// Default timeout in seconds.
    /// Per FR-002b-103.
    /// </summary>
    public const int DefaultTimeoutSeconds = 120;

    /// <summary>
    /// Default retry count.
    /// Per FR-002b-104.
    /// </summary>
    public const int DefaultRetryCount = 3;
}
