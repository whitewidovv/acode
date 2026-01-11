using Acode.Application.Models;
using Acode.Domain.Models.Routing;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Models;

/// <summary>
/// Orchestrates routing strategy selection and fallback handling.
/// </summary>
/// <remarks>
/// The routing policy is responsible for:
/// - Selecting the appropriate routing strategy based on configuration.
/// - Delegating routing decisions to the selected strategy.
/// - Handling fallback when primary models are unavailable.
/// - Providing model availability information.
///
/// Supported strategies:
/// - "single": Routes all requests to a single default model.
/// - "role-based": Routes requests based on role-to-model mappings.
///
/// Fallback behavior:
/// - If the primary model is unavailable, tries the fallback chain.
/// - If all models are unavailable, throws InvalidOperationException.
/// </remarks>
public sealed class RoutingPolicy : IRoutingPolicy
{
    private readonly ILogger<RoutingPolicy> _logger;
    private readonly IModelAvailabilityChecker _availabilityChecker;
    private readonly IRoutingStrategy _strategy;
    private readonly FallbackHandler _fallbackHandler;
    private readonly RoutingConfig _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoutingPolicy"/> class.
    /// </summary>
    /// <param name="logger">Logger for routing decisions.</param>
    /// <param name="availabilityChecker">Model availability checker.</param>
    /// <param name="config">Routing configuration.</param>
    /// <param name="strategy">Routing strategy to use.</param>
    /// <param name="fallbackHandler">Fallback handler for unavailable models.</param>
    public RoutingPolicy(
        ILogger<RoutingPolicy> logger,
        IModelAvailabilityChecker availabilityChecker,
        RoutingConfig config,
        IRoutingStrategy strategy,
        FallbackHandler fallbackHandler)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(availabilityChecker);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(strategy);
        ArgumentNullException.ThrowIfNull(fallbackHandler);

        _logger = logger;
        _availabilityChecker = availabilityChecker;
        _config = config;
        _strategy = strategy;
        _fallbackHandler = fallbackHandler;
    }

    /// <inheritdoc/>
    public RoutingDecision GetModel(RoutingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Get initial routing decision from strategy
        var decision = _strategy.GetModel(request);

        // Check if the selected model is available
        if (!_availabilityChecker.IsModelAvailable(decision.ModelId))
        {
            _logger.LogWarning(
                "Primary model {ModelId} unavailable, attempting fallback",
                decision.ModelId);

            // Try fallback chain
            if (_fallbackHandler.TryFallback(decision.ModelId, _config, out var fallbackModel))
            {
                return new RoutingDecision
                {
                    ModelId = fallbackModel!,
                    IsFallback = true,
                    Reason = $"Fallback from {decision.ModelId} → {fallbackModel}",
                };
            }

            // No fallback available
            var availableModels = _availabilityChecker.ListAvailableModels();
            throw new InvalidOperationException(
                $"No suitable model available. Requested: {decision.ModelId}. " +
                $"Available models: {string.Join(", ", availableModels)}");
        }

        return decision;
    }

    /// <inheritdoc/>
    public bool IsModelAvailable(string modelId)
    {
        return _availabilityChecker.IsModelAvailable(modelId);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> ListAvailableModels()
    {
        return _availabilityChecker.ListAvailableModels();
    }
}
