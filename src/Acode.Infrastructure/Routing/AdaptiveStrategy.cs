namespace Acode.Infrastructure.Routing;

using Acode.Application.Routing;
using Microsoft.Extensions.Logging;

/// <summary>
/// Adaptive strategy—dynamically selects models based on task complexity.
/// </summary>
/// <remarks>
/// Future enhancement: full complexity-aware implementation.
/// Currently delegates to role-based selection with complexity hints.
/// </remarks>
internal sealed class AdaptiveStrategy : IRoutingStrategy
{
    private readonly RoutingConfiguration _configuration;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdaptiveStrategy"/> class.
    /// </summary>
    /// <param name="configuration">The routing configuration.</param>
    /// <param name="logger">The logger.</param>
    public AdaptiveStrategy(RoutingConfiguration configuration, ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string SelectModel(AgentRole role, RoutingContext context)
    {
        // Adaptive strategy considers task complexity
        var complexity = context.TaskComplexity ?? TaskComplexity.Unknown;

        _logger.LogDebug(
            "Adaptive strategy: evaluating role {Role} with complexity {Complexity}",
            role,
            complexity
        );

        // For high complexity tasks, prefer large models regardless of role
        if (complexity == TaskComplexity.High)
        {
            // Try to find the largest model in fallback chain
            if (_configuration.FallbackChain.Count > 0)
            {
                var largestModel = _configuration.FallbackChain[0];
                _logger.LogDebug(
                    "Adaptive strategy: high complexity, selecting largest model {Model}",
                    largestModel
                );
                return largestModel;
            }
        }

        // For low complexity, prefer smaller/faster models
        if (complexity == TaskComplexity.Low)
        {
            // Use the last (smallest) model in fallback chain, or default
            if (_configuration.FallbackChain.Count > 0)
            {
                var smallestModel = _configuration.FallbackChain[^1];
                _logger.LogDebug(
                    "Adaptive strategy: low complexity, selecting smallest model {Model}",
                    smallestModel
                );
                return smallestModel;
            }
        }

        // Default: use role-based selection
        if (_configuration.RoleModels.TryGetValue(role, out var modelId))
        {
            _logger.LogDebug(
                "Adaptive strategy: using role-based selection, {Model} for {Role}",
                modelId,
                role
            );
            return modelId;
        }

        _logger.LogDebug(
            "Adaptive strategy: falling back to default model {Model}",
            _configuration.DefaultModel
        );

        return _configuration.DefaultModel;
    }
}
