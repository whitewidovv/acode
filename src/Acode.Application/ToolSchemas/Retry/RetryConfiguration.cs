namespace Acode.Application.ToolSchemas.Retry;

/// <summary>
/// Configuration options for the retry contract.
/// Bind from .agent/config.yml tools.validation section.
/// </summary>
/// <remarks>
/// Spec Reference: Implementation Prompt lines 3438-3505.
/// All properties have sensible defaults for immediate use without configuration.
/// </remarks>
public sealed class RetryConfiguration
{
    /// <summary>
    /// Gets or sets the maximum retry attempts before escalation.
    /// Default: 3. Range: 1-10.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the maximum error message length in characters.
    /// Default: 2000. Range: 500-4000.
    /// </summary>
    public int MaxMessageLength { get; set; } = 2000;

    /// <summary>
    /// Gets or sets the maximum number of errors shown per validation.
    /// Default: 10. Additional errors summarized as "...and N more".
    /// </summary>
    public int MaxErrorsShown { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum length of actual value preview in characters.
    /// Default: 100. Longer values truncated with "...".
    /// </summary>
    public int MaxValuePreview { get; set; } = 100;

    /// <summary>
    /// Gets or sets a value indicating whether to include correction hints in error messages.
    /// Default: true.
    /// </summary>
    public bool IncludeHints { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include actual values in error messages.
    /// Default: true. Set false to reduce token usage.
    /// </summary>
    public bool IncludeActualValues { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to track retry history in memory.
    /// Default: true. Set false to save memory in high-scale scenarios.
    /// </summary>
    public bool TrackHistory { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to redact detected secrets in actual values.
    /// Default: true.
    /// </summary>
    public bool RedactSecrets { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to convert absolute file paths to relative.
    /// Default: true.
    /// </summary>
    public bool RelativizePaths { get; set; } = true;
}
