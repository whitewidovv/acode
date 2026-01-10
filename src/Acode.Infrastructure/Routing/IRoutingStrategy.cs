namespace Acode.Infrastructure.Routing;

using Acode.Application.Routing;

/// <summary>
/// Defines the contract for routing strategy implementations.
/// </summary>
/// <remarks>
/// Strategy pattern enables different routing algorithms without modifying core policy.
/// </remarks>
internal interface IRoutingStrategy
{
    /// <summary>
    /// Selects a model for the specified role and context.
    /// </summary>
    /// <param name="role">The agent role.</param>
    /// <param name="context">The routing context.</param>
    /// <returns>The selected model identifier.</returns>
    string SelectModel(AgentRole role, RoutingContext context);
}
