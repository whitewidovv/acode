namespace Acode.Application.Routing;

using System.Collections.Generic;

/// <summary>
/// Configuration for the routing policy.
/// </summary>
/// <remarks>
/// AC-017 to AC-022: Configuration schema for models.routing section.
/// </remarks>
public sealed class RoutingConfiguration
{
    /// <summary>
    /// Gets or sets the routing strategy to use.
    /// </summary>
    /// <remarks>
    /// AC-018, AC-019: Strategy field, defaults to "single".
    /// </remarks>
    public RoutingStrategy Strategy { get; set; } = RoutingStrategy.SingleModel;

    /// <summary>
    /// Gets or sets the default model used when no role-specific model is configured.
    /// </summary>
    /// <remarks>
    /// AC-020: Default model field.
    /// </remarks>
    public string DefaultModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets the role-to-model mapping for role-based strategy.
    /// </summary>
    /// <remarks>
    /// AC-021: Role models map.
    /// </remarks>
    public Dictionary<AgentRole, string> RoleModels { get; init; } = new();

    /// <summary>
    /// Gets the ordered fallback chain for when primary models are unavailable.
    /// </summary>
    /// <remarks>
    /// AC-022, AC-030: Fallback chain configuration.
    /// </remarks>
    public List<string> FallbackChain { get; init; } = new();

    /// <summary>
    /// Gets or sets the availability cache TTL in seconds.
    /// </summary>
    /// <remarks>
    /// Default 5 seconds per design decision.
    /// </remarks>
    public int AvailabilityCacheTtlSeconds { get; set; } = 5;
}
