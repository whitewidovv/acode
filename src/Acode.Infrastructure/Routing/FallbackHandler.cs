namespace Acode.Infrastructure.Routing;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Acode.Application.Routing;
using Acode.Domain.Modes;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implements fallback logic when primary model is unavailable.
/// </summary>
/// <remarks>
/// AC-030 to AC-034: Fallback chain handling.
/// </remarks>
public sealed class FallbackHandler
{
    private readonly RoutingConfiguration _configuration;
    private readonly ModelRegistry _modelRegistry;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FallbackHandler"/> class.
    /// </summary>
    /// <param name="configuration">The routing configuration.</param>
    /// <param name="modelRegistry">The model registry.</param>
    /// <param name="logger">The logger.</param>
    public FallbackHandler(
        RoutingConfiguration configuration,
        ModelRegistry modelRegistry,
        ILogger logger
    )
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _modelRegistry = modelRegistry ?? throw new ArgumentNullException(nameof(modelRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles fallback when primary model is unavailable.
    /// </summary>
    /// <param name="role">The agent role.</param>
    /// <param name="context">The routing context.</param>
    /// <param name="primaryModelId">The primary model that was unavailable.</param>
    /// <param name="stopwatch">Stopwatch for timing.</param>
    /// <returns>A routing decision with fallback model.</returns>
    /// <exception cref="RoutingException">Thrown when all fallback models are exhausted.</exception>
    public RoutingDecision HandleFallback(
        AgentRole role,
        RoutingContext context,
        string? primaryModelId,
        Stopwatch stopwatch
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(stopwatch);

        if (_configuration.FallbackChain == null || _configuration.FallbackChain.Count == 0)
        {
            throw new RoutingException(
                "ACODE-RTE-004",
                $"No available model for role {role} and no fallback chain configured",
                primaryModelId != null ? new[] { primaryModelId } : Array.Empty<string>()
            )
            {
                Suggestion = "Configure a fallback_chain in routing configuration",
            };
        }

        var attemptedModels =
            primaryModelId != null ? new List<string> { primaryModelId } : new List<string>();

        // Traverse fallback chain sequentially (AC-031)
        foreach (var fallbackModelId in _configuration.FallbackChain)
        {
            if (attemptedModels.Contains(fallbackModelId))
            {
                continue; // Skip if already attempted
            }

            attemptedModels.Add(fallbackModelId);

            _logger.LogDebug("Checking fallback model {ModelId}", fallbackModelId);

            // Validate operating mode constraint
            var modelInfo = _modelRegistry.GetModelInfo(fallbackModelId);
            if (modelInfo != null && !ValidateOperatingMode(modelInfo, context.OperatingMode))
            {
                _logger.LogDebug(
                    "Fallback model {ModelId} rejected by operating mode constraint",
                    fallbackModelId
                );
                continue;
            }

            // Check availability (AC-032: first available selected)
            if (_modelRegistry.IsModelAvailable(fallbackModelId))
            {
                stopwatch.Stop();

                // AC-034, AC-051: Fallback is logged as warning
                _logger.LogWarning(
                    "Fallback activated: primary={Primary}, fallback={Fallback}, role={Role}",
                    primaryModelId ?? "none",
                    fallbackModelId,
                    role
                );

                return new RoutingDecision
                {
                    ModelId = fallbackModelId,
                    IsFallback = true,
                    FallbackReason = "primary_unavailable",
                    SelectionReason = $"fallback from {primaryModelId ?? "none"}",
                    SelectedProvider = _modelRegistry.GetProviderForModel(fallbackModelId),
                    DecisionTimeMs = stopwatch.ElapsedMilliseconds,
                    Timestamp = DateTime.UtcNow,
                };
            }
        }

        // AC-033: All unavailable fails
        throw new RoutingException(
            "ACODE-RTE-004",
            $"Fallback chain exhausted for role {role}. No available models.",
            attemptedModels.ToArray()
        )
        {
            Suggestion = $"Start a model with 'ollama run {_configuration.FallbackChain.Last()}'",
        };
    }

    private static bool ValidateOperatingMode(ModelInfo modelInfo, OperatingMode operatingMode)
    {
        return operatingMode switch
        {
            OperatingMode.LocalOnly => modelInfo.IsLocal,
            OperatingMode.Airgapped => modelInfo.IsLocal,
            OperatingMode.Burst => true,
            _ => true,
        };
    }
}
