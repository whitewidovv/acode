using Acode.Domain.Models.Routing;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Models;

/// <summary>
/// Routes requests based on role-to-model mappings defined in configuration.
/// </summary>
/// <remarks>
/// This strategy uses the RoleModels dictionary in RoutingConfig to map
/// each role (planner, coder, reviewer) to a specific model.
///
/// Role name matching is case-insensitive for flexibility.
///
/// Fallback behavior:
/// - If a role has no mapping in RoleModels, use DefaultModel (IsFallback = true).
/// - If RoleModels is null or empty, use DefaultModel (IsFallback = true).
///
/// This strategy is selected when RoutingConfig.Strategy = "role-based".
/// </remarks>
public sealed class RoleBasedStrategy : IRoutingStrategy
{
    private readonly ILogger<RoleBasedStrategy> _logger;
    private readonly RoutingConfig _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleBasedStrategy"/> class.
    /// </summary>
    /// <param name="logger">Logger for routing decisions.</param>
    /// <param name="config">Routing configuration containing role-to-model mappings.</param>
    public RoleBasedStrategy(ILogger<RoleBasedStrategy> logger, RoutingConfig config)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(config);

        _logger = logger;
        _config = config;
    }

    /// <inheritdoc/>
    public RoutingDecision GetModel(RoutingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Convert role enum to lowercase string for case-insensitive lookup
        var roleName = request.Role.ToString().ToLowerInvariant();

        // Try to find role-specific model
        if (_config.RoleModels != null)
        {
            // Case-insensitive lookup
            var roleModelEntry = _config.RoleModels.FirstOrDefault(kvp =>
                kvp.Key.Equals(roleName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(roleModelEntry.Key))
            {
                var decision = new RoutingDecision
                {
                    ModelId = roleModelEntry.Value,
                    IsFallback = false,
                    Reason = $"role-based routing: {roleName} role → {roleModelEntry.Value}",
                };

                _logger.LogInformation(
                    "Routing decision: Role={Role}, Model={Model}, Strategy=role-based",
                    request.Role,
                    decision.ModelId);

                return decision;
            }
        }

        // Fallback to default model
        var fallbackDecision = new RoutingDecision
        {
            ModelId = _config.DefaultModel,
            IsFallback = true,
            Reason = $"role-based routing: no mapping for {roleName}, using default {_config.DefaultModel}",
        };

        _logger.LogInformation(
            "Routing decision (fallback): Role={Role}, Model={Model}, Strategy=role-based",
            request.Role,
            fallbackDecision.ModelId);

        return fallbackDecision;
    }
}
