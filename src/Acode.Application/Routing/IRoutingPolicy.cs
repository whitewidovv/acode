namespace Acode.Application.Routing;

using System.Collections.Generic;

/// <summary>
/// Defines the contract for routing agent roles to appropriate models based on
/// configuration, availability, and operating mode constraints.
/// </summary>
/// <remarks>
/// The routing policy enables heterogeneous model usage—different models for
/// different agent roles. This optimizes for both quality (large models for planning)
/// and performance (small models for coding). The policy respects operating mode
/// constraints and handles model unavailability through fallback chains.
///
/// AC-001: IRoutingPolicy interface in Application layer.
/// AC-002 to AC-006: Required methods defined.
/// </remarks>
public interface IRoutingPolicy
{
    /// <summary>
    /// Selects the appropriate model for the specified agent role and context.
    /// </summary>
    /// <param name="role">The agent role requesting a model (planner, coder, reviewer).</param>
    /// <param name="context">Context for routing decision including operating mode and complexity.</param>
    /// <returns>
    /// A RoutingDecision containing the selected model ID, fallback status, and selection reason.
    /// </returns>
    /// <exception cref="RoutingException">
    /// Thrown when no available model satisfies the routing constraints (operating mode,
    /// capabilities, availability). Error includes attempted models and remediation suggestion.
    /// </exception>
    /// <remarks>
    /// AC-002: GetModel method exists.
    /// AC-003: Returns ModelConfiguration (via RoutingDecision).
    /// </remarks>
    /// <example>
    /// <code>
    /// var context = new RoutingContext { OperatingMode = OperatingMode.LocalOnly };
    /// var decision = routingPolicy.GetModel(AgentRole.Planner, context);
    /// // decision.ModelId might be "llama3.2:70b"
    /// </code>
    /// </example>
    RoutingDecision GetModel(AgentRole role, RoutingContext context);

    /// <summary>
    /// Attempts to find a fallback model when the primary model is unavailable.
    /// </summary>
    /// <param name="role">The agent role requesting a fallback model.</param>
    /// <param name="context">Context for fallback decision.</param>
    /// <returns>A RoutingDecision indicating the fallback model or null if no fallback available.</returns>
    /// <remarks>
    /// AC-004: GetFallbackModel method exists.
    /// </remarks>
    RoutingDecision? GetFallbackModel(AgentRole role, RoutingContext context);

    /// <summary>
    /// Checks whether the specified model is currently available for inference.
    /// </summary>
    /// <param name="modelId">The model identifier in name:tag or name:tag@provider format.</param>
    /// <returns>True if the model is loaded and ready for inference, false otherwise.</returns>
    /// <remarks>
    /// Availability checks are cached for performance (default 5 second TTL). This method
    /// queries the model provider registry and returns cached results when available.
    /// AC-005: IsModelAvailable method exists.
    /// </remarks>
    bool IsModelAvailable(string modelId);

    /// <summary>
    /// Returns a list of all models currently available across all registered providers.
    /// </summary>
    /// <returns>Read-only list of available models with metadata (parameter count, capabilities).</returns>
    /// <remarks>
    /// AC-006: ListAvailableModels method exists.
    /// </remarks>
    IReadOnlyList<ModelInfo> ListAvailableModels();
}
