namespace Acode.Domain.Models.Routing;

/// <summary>
/// Configuration for model routing policy.
/// </summary>
/// <remarks>
/// Routing configuration is defined in .agent/config.yml under models.routing section.
/// This model represents the parsed and validated routing configuration.
/// </remarks>
public sealed record RoutingConfig
{
    /// <summary>
    /// Gets the routing strategy to use.
    /// </summary>
    /// <remarks>
    /// Valid values: "single", "role-based", "adaptive".
    /// Default: "single".
    /// </remarks>
    public string Strategy { get; init; } = "single";

    /// <summary>
    /// Gets the default model to use when no role-specific model is configured.
    /// </summary>
    /// <remarks>
    /// Required for all strategies.
    /// Example: "llama3.2:7b".
    /// </remarks>
    public string DefaultModel { get; init; } = "llama3.2:7b";

    /// <summary>
    /// Gets the role-to-model mappings for role-based strategy.
    /// </summary>
    /// <remarks>
    /// Only used when Strategy = "role-based".
    /// Keys: "planner", "coder", "reviewer".
    /// Values: Model IDs (e.g., "llama3.2:70b").
    /// </remarks>
    public IReadOnlyDictionary<string, string>? RoleModels { get; init; }

    /// <summary>
    /// Gets the fallback chain for when primary models are unavailable.
    /// </summary>
    /// <remarks>
    /// Ordered list of model IDs to try sequentially.
    /// Example: ["llama3.2:70b", "llama3.2:7b", "mistral:7b"].
    /// </remarks>
    public IReadOnlyList<string>? FallbackChain { get; init; }
}
