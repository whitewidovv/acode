namespace Acode.Infrastructure.Routing;

using Acode.Application.Routing;
using Microsoft.Extensions.Logging;

/// <summary>
/// Role-based strategy—assigns different models to different roles.
/// </summary>
/// <remarks>
/// AC-026 to AC-029: Role-based strategy implementation.
/// </remarks>
internal sealed class RoleBasedStrategy : IRoutingStrategy
{
    private readonly RoutingConfiguration _configuration;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleBasedStrategy"/> class.
    /// </summary>
    /// <param name="configuration">The routing configuration.</param>
    /// <param name="logger">The logger.</param>
    public RoleBasedStrategy(RoutingConfiguration configuration, ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string SelectModel(AgentRole role, RoutingContext context)
    {
        // Check if role has a configured model
        if (_configuration.RoleModels.TryGetValue(role, out var modelId))
        {
            _logger.LogDebug(
                "Role-based strategy: selected {Model} for role {Role}",
                modelId,
                role
            );

            return modelId;
        }

        // Fall back to default model if role not configured
        _logger.LogDebug(
            "Role-based strategy: role {Role} not configured, using default {Model}",
            role,
            _configuration.DefaultModel
        );

        return _configuration.DefaultModel;
    }
}
