using Acode.Domain.Models.Routing;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Models;

/// <summary>
/// Routes all requests to a single configured model regardless of role.
/// </summary>
/// <remarks>
/// This is the simplest routing strategy - it ignores the agent role and
/// always returns the default model configured in <see cref="RoutingConfig.DefaultModel"/>.
///
/// Use this strategy when:
/// - Running with a single model (e.g., only llama3.2:7b available).
/// - Testing or development with limited models.
/// - You want consistent behavior across all roles.
///
/// This strategy is selected when RoutingConfig.Strategy = "single".
/// </remarks>
public sealed class SingleModelStrategy : IRoutingStrategy
{
    private readonly ILogger<SingleModelStrategy> _logger;
    private readonly RoutingConfig _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleModelStrategy"/> class.
    /// </summary>
    /// <param name="logger">Logger for routing decisions.</param>
    /// <param name="config">Routing configuration containing the default model.</param>
    public SingleModelStrategy(ILogger<SingleModelStrategy> logger, RoutingConfig config)
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

        var decision = new RoutingDecision
        {
            ModelId = _config.DefaultModel,
            IsFallback = false,
            Reason = $"single model strategy: {_config.DefaultModel} for all roles",
        };

        _logger.LogInformation(
            "Routing decision: Role={Role}, Model={Model}, Strategy=single",
            request.Role,
            decision.ModelId);

        return decision;
    }
}
