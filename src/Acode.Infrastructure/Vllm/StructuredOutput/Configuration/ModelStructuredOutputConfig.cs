namespace Acode.Infrastructure.Vllm.StructuredOutput.Configuration;

/// <summary>
/// Per-model structured output configuration.
/// </summary>
public sealed class ModelStructuredOutputConfig
{
    /// <summary>
    /// Gets or sets a value indicating whether structured output is enabled for this model.
    /// Default: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the fallback configuration for this model.
    /// </summary>
    public FallbackConfiguration? Fallback { get; set; }

    /// <summary>
    /// Gets or sets the supported modes for this model.
    /// If null, all modes are assumed supported.
    /// </summary>
    public string[]? SupportedModes { get; set; }
}
