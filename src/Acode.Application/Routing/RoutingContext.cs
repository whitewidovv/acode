namespace Acode.Application.Routing;

using System.Collections.Generic;
using Acode.Domain.Modes;

/// <summary>
/// Provides context for routing decisions including operating mode constraints,
/// task complexity, and user overrides.
/// </summary>
/// <remarks>
/// FR-009: Context object for routing decision input.
/// </remarks>
public sealed class RoutingContext
{
    /// <summary>
    /// Gets the operating mode constraint (local-only, air-gapped, burst).
    /// Routing policy enforces mode constraints before model selection.
    /// </summary>
    /// <remarks>
    /// AC-035 to AC-038: Operating mode constraints are enforced.
    /// </remarks>
    public required OperatingMode OperatingMode { get; init; }

    /// <summary>
    /// Gets the estimated task complexity.
    /// Used by adaptive routing strategy to select model based on difficulty.
    /// </summary>
    public TaskComplexity? TaskComplexity { get; init; }

    /// <summary>
    /// Gets the estimated token count for the task.
    /// Used for complexity estimation.
    /// </summary>
    public int? EstimatedTokenCount { get; init; }

    /// <summary>
    /// Gets the user-specified model override.
    /// When set, routing policy bypasses configured strategy and uses this model
    /// (subject to operating mode constraints).
    /// </summary>
    /// <remarks>
    /// AC-029: User overrides bypass strategy but respect mode constraints.
    /// </remarks>
    public string? UserOverride { get; init; }

    /// <summary>
    /// Gets the required model capabilities for this task.
    /// Routing policy only selects models that support all required capabilities.
    /// </summary>
    public IReadOnlyList<ModelCapability>? RequiredCapabilities { get; init; }
}
