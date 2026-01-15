namespace Acode.Infrastructure.Vllm.StructuredOutput.Capability;

/// <summary>
/// Detects structured output capabilities for vLLM models.
/// </summary>
/// <remarks>
/// FR-035 through FR-039: Model capability detection for guided decoding modes.
/// </remarks>
public sealed class CapabilityDetector
{
    private const int DefaultMaxSchemaSizeBytes = 65536; // 64KB
    private const int DefaultMaxSchemaDepth = 10;

    /// <summary>
    /// Detects capabilities for a given model.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <returns>Detected model capabilities.</returns>
    /// <remarks>
    /// Heuristics:
    /// - All supported vLLM models support guided_json.
    /// - Models from 2024 onwards typically support guided_choice.
    /// - Recent models typically support guided_regex.
    /// - Capability levels are conservative (assume minimum support by default).
    /// </remarks>
    public ModelCapabilities DetectCapabilities(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return this.CreateMinimalCapabilities(modelId ?? string.Empty);
        }

        var capabilities = new ModelCapabilities
        {
            ModelId = modelId,
            SupportsGuidedJson = true, // All supported models have this
            SupportsGuidedChoice = this.IsRecentModel(modelId),
            SupportsGuidedRegex = this.IsRecentModel(modelId),
            MaxSchemaSizeBytes = DefaultMaxSchemaSizeBytes,
            MaxSchemaDepth = DefaultMaxSchemaDepth,
            LastDetectedUtc = DateTime.UtcNow,
            IsStale = false,
        };

        return capabilities;
    }

    /// <summary>
    /// Detects capabilities asynchronously (placeholder for future API calls).
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, with detected capabilities.</returns>
    public async Task<ModelCapabilities> DetectCapabilitiesAsync(string modelId, CancellationToken cancellationToken = default)
    {
        // Simulate async detection (no I/O in current implementation)
        await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        return this.DetectCapabilities(modelId);
    }

    /// <summary>
    /// Marks capabilities as stale for refresh.
    /// </summary>
    /// <param name="capabilities">Capabilities to mark as stale.</param>
    public void MarkAsStale(ModelCapabilities capabilities)
    {
        if (capabilities != null)
        {
            capabilities.IsStale = true;
        }
    }

    /// <summary>
    /// Checks if capabilities require refresh based on age.
    /// </summary>
    /// <param name="capabilities">Capabilities to check.</param>
    /// <param name="refreshIntervalMinutes">Age threshold in minutes (default 60).</param>
    /// <returns>True if capabilities are stale and need refresh.</returns>
    public bool RequiresRefresh(ModelCapabilities capabilities, int refreshIntervalMinutes = 60)
    {
        if (capabilities == null || capabilities.IsStale)
        {
            return true;
        }

        var age = DateTime.UtcNow - capabilities.LastDetectedUtc;
        return age.TotalMinutes > refreshIntervalMinutes;
    }

    private ModelCapabilities CreateMinimalCapabilities(string modelId)
    {
        return new ModelCapabilities
        {
            ModelId = modelId,
            SupportsGuidedJson = false,
            SupportsGuidedChoice = false,
            SupportsGuidedRegex = false,
            MaxSchemaSizeBytes = 0,
            MaxSchemaDepth = 0,
            LastDetectedUtc = DateTime.UtcNow,
            IsStale = true,
        };
    }

    private bool IsRecentModel(string modelId)
    {
        // Simple heuristic: model IDs containing "2024", "2025", or version numbers >= 7
        // suggest more recent models with broader support
        if (string.IsNullOrEmpty(modelId))
        {
            return false;
        }

        var lowerModelId = modelId.ToLowerInvariant();
        return lowerModelId.Contains("2024", StringComparison.Ordinal)
            || lowerModelId.Contains("2025", StringComparison.Ordinal)
            || lowerModelId.Contains("llama2", StringComparison.Ordinal) // Llama 2 and newer support more features
            || lowerModelId.Contains("llama3", StringComparison.Ordinal)
            || lowerModelId.Contains("mistral", StringComparison.Ordinal)
            || lowerModelId.Contains("neural", StringComparison.Ordinal);
    }
}
