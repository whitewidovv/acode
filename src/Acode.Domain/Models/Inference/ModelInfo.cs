using Acode.Domain.Modes;

namespace Acode.Domain.Models.Inference;

/// <summary>
/// Metadata about an available model from a provider.
/// </summary>
/// <remarks>
/// FR-004-19: Model metadata includes local vs remote information.
/// FR-009c-081 to FR-009c-085: Operating mode constraints use this metadata.
/// </remarks>
public sealed record ModelInfo
{
    /// <summary>
    /// Gets the unique identifier for the model (e.g., "llama3.2:7b").
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Gets a value indicating whether the model is hosted locally (true) or remotely (false).
    /// </summary>
    /// <remarks>
    /// - Ollama models: IsLocal = true.
    /// - vLLM with local backend: IsLocal = true.
    /// - vLLM with remote backend: IsLocal = false.
    /// </remarks>
    public required bool IsLocal { get; init; }

    /// <summary>
    /// Gets a value indicating whether the model requires network access to function.
    /// </summary>
    /// <remarks>
    /// - Ollama (local): RequiresNetwork = false.
    /// - vLLM (local backend): RequiresNetwork = false (if no remote deps).
    /// - vLLM (remote backend): RequiresNetwork = true.
    /// - Cloud models: RequiresNetwork = true.
    /// </remarks>
    public required bool RequiresNetwork { get; init; }

    /// <summary>
    /// Checks if this model is allowed in the specified operating mode.
    /// </summary>
    /// <param name="mode">The operating mode to check against.</param>
    /// <returns>True if the model is allowed, false otherwise.</returns>
    /// <remarks>
    /// Operating mode constraints:
    /// - LocalOnly: Requires IsLocal = true.
    /// - Airgapped: Requires RequiresNetwork = false.
    /// - Burst: Allows all models.
    ///
    /// FR-009c-081: MUST respect OperatingMode constraints.
    /// FR-009c-082: LocalOnly MUST exclude network models.
    /// FR-009c-083: Airgapped MUST exclude all network models.
    /// FR-009c-084: Burst MAY include cloud models if configured.
    /// </remarks>
    public bool IsAllowedInMode(OperatingMode mode)
    {
        return mode switch
        {
            OperatingMode.LocalOnly => IsLocal,
            OperatingMode.Airgapped => !RequiresNetwork,
            OperatingMode.Burst => true,
            _ => false,
        };
    }
}
