namespace Acode.Infrastructure.Vllm.StructuredOutput.Configuration;

/// <summary>
/// Configuration for fallback behavior when structured output is unavailable.
/// </summary>
/// <remarks>
/// FR-007e: Fallback configuration for models without guided decoding support.
/// </remarks>
public sealed class FallbackConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether fallback is enabled.
    /// Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum retry attempts in fallback mode.
    /// Default: 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the validation mode in fallback.
    /// "strict" = same as guided decoding (no extra fields), "lenient" = allow extra fields.
    /// Default: strict.
    /// </summary>
    public string ValidationMode { get; set; } = "strict";

    /// <summary>
    /// Gets or sets the delay between retry attempts in milliseconds.
    /// Default: 100.
    /// </summary>
    public int RetryDelayMs { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating whether to use exponential backoff for retries.
    /// Default: true.
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum delay between retries in milliseconds.
    /// Default: 1000.
    /// </summary>
    public int MaxRetryDelayMs { get; set; } = 1000;
}
