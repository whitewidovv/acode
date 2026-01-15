namespace Acode.Infrastructure.Vllm.StructuredOutput.Capability;

/// <summary>
/// Represents the structured output capabilities of a model.
/// </summary>
/// <remarks>
/// FR-040 through FR-043: Model capability tracking for structured output enforcement.
/// </remarks>
public sealed class ModelCapabilities
{
    /// <summary>
    /// Gets or sets the model identifier.
    /// </summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the model supports guided_json mode.
    /// </summary>
    public bool SupportsGuidedJson { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports guided_choice mode.
    /// </summary>
    public bool SupportsGuidedChoice { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the model supports guided_regex mode.
    /// </summary>
    public bool SupportsGuidedRegex { get; set; }

    /// <summary>
    /// Gets or sets the maximum schema size (in bytes) that the model can handle.
    /// </summary>
    public int MaxSchemaSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the maximum schema depth that the model can handle.
    /// </summary>
    public int MaxSchemaDepth { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when capabilities were last detected.
    /// </summary>
    public DateTime LastDetectedUtc { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the capabilities are stale (require refresh).
    /// </summary>
    public bool IsStale { get; set; }
}
