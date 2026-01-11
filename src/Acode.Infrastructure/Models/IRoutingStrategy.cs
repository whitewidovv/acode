using Acode.Domain.Models.Routing;

namespace Acode.Infrastructure.Models;

/// <summary>
/// Defines the contract for routing strategy implementations.
/// </summary>
/// <remarks>
/// Routing strategies determine which model should handle a request based on
/// different algorithms (single-model, role-based, adaptive).
///
/// Strategies are used internally by routing policy implementations
/// to make routing decisions according to configured behavior.
/// </remarks>
public interface IRoutingStrategy
{
    /// <summary>
    /// Selects a model for the specified routing request.
    /// </summary>
    /// <param name="request">The routing request containing role and context.</param>
    /// <returns>A routing decision containing the selected model ID and reasoning.</returns>
    RoutingDecision GetModel(RoutingRequest request);
}
