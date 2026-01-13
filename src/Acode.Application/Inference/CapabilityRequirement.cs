namespace Acode.Application.Inference;

/// <summary>
/// Describes capability requirements for a request.
/// </summary>
/// <remarks>
/// Used by ProviderCapabilities.Supports() method (FR-036).
/// </remarks>
public sealed record CapabilityRequirement
{
    /// <summary>
    /// Gets a value indicating whether streaming is required.
    /// </summary>
    public bool RequiresStreaming { get; init; }

    /// <summary>
    /// Gets a value indicating whether tool/function calling is required.
    /// </summary>
    public bool RequiresToolCalls { get; init; }

    /// <summary>
    /// Gets a value indicating whether JSON mode is required.
    /// </summary>
    public bool RequiresJsonMode { get; init; }

    /// <summary>
    /// Gets the minimum context length required in tokens.
    /// </summary>
    public int? MinContextTokens { get; init; }

    /// <summary>
    /// Gets the minimum output length required in tokens.
    /// </summary>
    public int? MinOutputTokens { get; init; }

    /// <summary>
    /// Gets the required model name (if specific model needed).
    /// </summary>
    public string? RequiredModel { get; init; }
}
