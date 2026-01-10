namespace Acode.Infrastructure.Routing;

using Acode.Application.Routing;
using Microsoft.Extensions.Logging;

/// <summary>
/// Single model strategy—uses the same model for all roles.
/// </summary>
/// <remarks>
/// AC-023 to AC-025: Single strategy implementation.
/// </remarks>
internal sealed class SingleModelStrategy : IRoutingStrategy
{
    private readonly RoutingConfiguration _configuration;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleModelStrategy"/> class.
    /// </summary>
    /// <param name="configuration">The routing configuration.</param>
    /// <param name="logger">The logger.</param>
    public SingleModelStrategy(RoutingConfiguration configuration, ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string SelectModel(AgentRole role, RoutingContext context)
    {
        _logger.LogDebug(
            "Single model strategy: selecting default model {Model} for role {Role}",
            _configuration.DefaultModel,
            role
        );

        return _configuration.DefaultModel;
    }
}
