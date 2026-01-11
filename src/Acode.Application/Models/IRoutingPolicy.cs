using Acode.Domain.Models.Routing;

namespace Acode.Application.Models;

/// <summary>
/// Defines the contract for routing requests to appropriate models.
/// </summary>
/// <remarks>
/// The routing policy evaluates a routing request (role, context, overrides)
/// and returns a decision identifying which model should handle the request.
///
/// Implementations include:
/// - SingleModelStrategy: Route all requests to one model
/// - RoleBasedStrategy: Route based on role-to-model mapping
/// - AdaptiveStrategy (future): Route based on complexity heuristics
///
/// Routing decisions are deterministic - identical inputs always produce
/// identical outputs. This ensures reproducible behavior and simplifies testing.
/// </remarks>
public interface IRoutingPolicy
{
    /// <summary>
    /// Selects a model for the specified routing request.
    /// </summary>
    /// <param name="request">The routing request containing role and context.</param>
    /// <returns>
    /// A routing decision containing the selected model ID, fallback status, and reason.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no suitable model can be found (all models unavailable).
    /// </exception>
    /// <remarks>
    /// Selection process:
    /// 1. If request.UserOverride is set, validate and use it (if available)
    /// 2. Otherwise, apply strategy-specific routing logic
    /// 3. If primary model unavailable, try fallback chain
    /// 4. If all models unavailable, throw exception
    ///
    /// The decision is logged for audit purposes.
    /// Decision time must be &lt; 10ms for 99% of requests.
    /// </remarks>
    RoutingDecision GetModel(RoutingRequest request);

    /// <summary>
    /// Checks if a specific model is currently available.
    /// </summary>
    /// <param name="modelId">The model identifier to check.</param>
    /// <returns>True if the model is loaded and ready; false otherwise.</returns>
    /// <remarks>
    /// Availability is cached for 5 seconds to reduce provider query overhead.
    /// This method queries the model provider registry to determine if a model
    /// is currently loaded and ready for inference.
    /// </remarks>
    bool IsModelAvailable(string modelId);

    /// <summary>
    /// Lists all models currently available for routing.
    /// </summary>
    /// <returns>Read-only list of available model IDs.</returns>
    /// <remarks>
    /// Returns only models that are:
    /// - Currently loaded in a provider (Ollama, vLLM)
    /// - Compatible with the current operating mode (local-only, air-gapped)
    ///
    /// The list is cached and refreshed every 5 seconds.
    /// </remarks>
    IReadOnlyList<string> ListAvailableModels();
}
