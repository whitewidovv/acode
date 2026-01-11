using Acode.Application.Models;
using Acode.Domain.Models.Routing;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Models;

/// <summary>
/// Handles fallback model selection when the primary model is unavailable.
/// </summary>
/// <remarks>
/// The fallback handler traverses the configured fallback chain sequentially,
/// checking each model for availability until one is found.
///
/// Fallback behavior:
/// - If primary model is available, no fallback occurs (returns false).
/// - If primary model is unavailable, tries each model in FallbackChain in order.
/// - If a fallback model is found, returns true with the fallback model ID.
/// - If all models are unavailable, returns false and logs available alternatives.
///
/// Each fallback attempt is logged for audit and debugging purposes.
/// </remarks>
public sealed class FallbackHandler
{
    private readonly ILogger<FallbackHandler> _logger;
    private readonly IModelAvailabilityChecker _availabilityChecker;

    /// <summary>
    /// Initializes a new instance of the <see cref="FallbackHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger for fallback attempts.</param>
    /// <param name="availabilityChecker">Model availability checker.</param>
    public FallbackHandler(ILogger<FallbackHandler> logger, IModelAvailabilityChecker availabilityChecker)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(availabilityChecker);

        _logger = logger;
        _availabilityChecker = availabilityChecker;
    }

    /// <summary>
    /// Attempts to find a fallback model when the primary model is unavailable.
    /// </summary>
    /// <param name="primaryModel">The primary model ID that was requested.</param>
    /// <param name="config">Routing configuration containing fallback chain.</param>
    /// <param name="fallbackModel">The fallback model ID if found; null otherwise.</param>
    /// <returns>True if a fallback model was found; false otherwise.</returns>
    public bool TryFallback(string primaryModel, RoutingConfig config, out string? fallbackModel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryModel);
        ArgumentNullException.ThrowIfNull(config);

        fallbackModel = null;

        // Check if primary model is available
        if (_availabilityChecker.IsModelAvailable(primaryModel))
        {
            // Primary model is available, no fallback needed
            return false;
        }

        // Primary model unavailable, try fallback chain
        if (config.FallbackChain == null || config.FallbackChain.Count == 0)
        {
            return false;
        }

        // Try each model in the fallback chain sequentially
        foreach (var candidateModel in config.FallbackChain)
        {
            _logger.LogInformation(
                "Trying fallback model: {FallbackModel} for unavailable primary {PrimaryModel}",
                candidateModel,
                primaryModel);

            if (_availabilityChecker.IsModelAvailable(candidateModel))
            {
                fallbackModel = candidateModel;
                _logger.LogInformation(
                    "Fallback successful: {FallbackModel} available for {PrimaryModel}",
                    fallbackModel,
                    primaryModel);

                return true;
            }
        }

        // All models in fallback chain are unavailable
        var availableModels = _availabilityChecker.ListAvailableModels();
        _logger.LogWarning(
            "All fallback models unavailable for {PrimaryModel}. Available models: {AvailableModels}",
            primaryModel,
            string.Join(", ", availableModels));

        return false;
    }
}
