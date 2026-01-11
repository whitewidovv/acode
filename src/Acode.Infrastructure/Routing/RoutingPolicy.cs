namespace Acode.Infrastructure.Routing;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Acode.Application.Routing;
using Acode.Domain.Modes;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implements the routing policy that selects appropriate models for agent roles.
/// </summary>
/// <remarks>
/// This is the main orchestrator that coordinates strategy implementations, fallback
/// handling, availability checking, and operating mode enforcement. It is registered
/// as a singleton service in dependency injection.
///
/// AC-007 to AC-012: Infrastructure layer implementation.
/// </remarks>
public sealed class RoutingPolicy : IRoutingPolicy
{
    private readonly RoutingConfiguration _configuration;
    private readonly ModelRegistry _modelRegistry;
    private readonly FallbackHandler _fallbackHandler;
    private readonly ILogger<RoutingPolicy> _logger;
    private readonly Dictionary<RoutingStrategy, IRoutingStrategy> _strategies;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoutingPolicy"/> class.
    /// </summary>
    /// <param name="configuration">The routing configuration.</param>
    /// <param name="modelRegistry">The model registry.</param>
    /// <param name="logger">The logger.</param>
    public RoutingPolicy(
        RoutingConfiguration configuration,
        ModelRegistry modelRegistry,
        ILogger<RoutingPolicy> logger
    )
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _modelRegistry = modelRegistry ?? throw new ArgumentNullException(nameof(modelRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _fallbackHandler = new FallbackHandler(_configuration, _modelRegistry, _logger);

        // Initialize routing strategies
        _strategies = new Dictionary<RoutingStrategy, IRoutingStrategy>
        {
            { RoutingStrategy.SingleModel, new SingleModelStrategy(_configuration, _logger) },
            { RoutingStrategy.RoleBased, new RoleBasedStrategy(_configuration, _logger) },
            { RoutingStrategy.Adaptive, new AdaptiveStrategy(_configuration, _logger) },
        };
    }

    /// <inheritdoc/>
    public RoutingDecision GetModel(AgentRole role, RoutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // AC-048: Request logged
            _logger.LogInformation(
                "Routing request for role {Role} with strategy {Strategy}",
                role,
                _configuration.Strategy
            );

            // Handle user override first (bypasses strategy but respects operating mode)
            if (!string.IsNullOrEmpty(context.UserOverride))
            {
                return HandleUserOverride(context.UserOverride, context, stopwatch);
            }

            // Select strategy and get primary model (AC-009, AC-010)
            var strategy = _strategies[_configuration.Strategy];
            var primaryModelId = strategy.SelectModel(role, context);

            // Validate model ID format
            if (!IsValidModelId(primaryModelId))
            {
                throw new RoutingException(
                    "ACODE-RTE-002",
                    $"Invalid model ID '{primaryModelId}'. Valid format: name:tag or name:tag@provider",
                    null
                );
            }

            // AC-011, AC-035 to AC-038: Check operating mode constraints
            var primaryModelInfo = _modelRegistry.GetModelInfo(primaryModelId);
            if (!ValidateOperatingModeConstraint(context.OperatingMode, primaryModelInfo))
            {
                throw new RoutingException(
                    "ACODE-RTE-003",
                    $"Model '{primaryModelId}' not allowed in {context.OperatingMode} mode",
                    new[] { primaryModelId }
                )
                {
                    Suggestion =
                        context.OperatingMode == OperatingMode.LocalOnly
                            ? "Use a local model or change operating mode to 'burst'"
                            : "Use an air-gapped model or change operating mode",
                };
            }

            // AC-039 to AC-042: Check model availability
            if (_modelRegistry.IsModelAvailable(primaryModelId))
            {
                stopwatch.Stop();

                var decision = new RoutingDecision
                {
                    ModelId = primaryModelId,
                    IsFallback = false,
                    SelectionReason = $"strategy: {_configuration.Strategy}, role: {role}",
                    SelectedProvider = _modelRegistry.GetProviderForModel(primaryModelId),
                    DecisionTimeMs = stopwatch.ElapsedMilliseconds,
                    Timestamp = DateTime.UtcNow,
                };

                // AC-012, AC-049, AC-050: Log decision
                LogRoutingDecision(decision, role);
                return decision;
            }

            // Primary unavailable, try fallback (AC-042)
            _logger.LogWarning(
                "Primary model {ModelId} unavailable, checking fallback chain",
                primaryModelId
            );

            return _fallbackHandler.HandleFallback(role, context, primaryModelId, stopwatch);
        }
        catch (RoutingException)
        {
            throw; // Rethrow routing exceptions as-is
        }
        catch (Exception ex)
        {
            throw new RoutingException(
                "ACODE-RTE-001",
                $"Routing failed for role {role}: {ex.Message}",
                null,
                ex
            );
        }
    }

    /// <inheritdoc/>
    public RoutingDecision? GetFallbackModel(AgentRole role, RoutingContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return _fallbackHandler.HandleFallback(role, context, null, stopwatch);
        }
        catch (RoutingException)
        {
            return null; // No fallback available
        }
    }

    /// <inheritdoc/>
    public bool IsModelAvailable(string modelId)
    {
        return _modelRegistry.IsModelAvailable(modelId);
    }

    /// <inheritdoc/>
    public IReadOnlyList<ModelInfo> ListAvailableModels()
    {
        return _modelRegistry.ListAvailableModels();
    }

    private static bool IsValidModelId(string modelId)
    {
        // Valid formats: "name:tag" or "name:tag@provider"
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return false;
        }

        var parts = modelId.Split('@');
        var modelPart = parts[0];

        return modelPart.Contains(':', StringComparison.Ordinal);
    }

    private static bool ValidateOperatingModeConstraint(
        OperatingMode operatingMode,
        ModelInfo? modelInfo
    )
    {
        if (modelInfo == null)
        {
            return true; // Model not in registry, assume constraint checking happens elsewhere
        }

        return operatingMode switch
        {
            OperatingMode.LocalOnly => modelInfo.IsLocal,
            OperatingMode.Airgapped => modelInfo.IsLocal,
            OperatingMode.Burst => true, // Burst allows any model
            _ => true,
        };
    }

    private RoutingDecision HandleUserOverride(
        string overrideModelId,
        RoutingContext context,
        Stopwatch stopwatch
    )
    {
        _logger.LogInformation("User override detected: {ModelId}", overrideModelId);

        // Validate model ID
        if (!IsValidModelId(overrideModelId))
        {
            throw new RoutingException(
                "ACODE-RTE-002",
                $"Invalid model ID in user override: '{overrideModelId}'",
                null
            );
        }

        // Still enforce operating mode constraints
        var overrideModelInfo = _modelRegistry.GetModelInfo(overrideModelId);
        if (!ValidateOperatingModeConstraint(context.OperatingMode, overrideModelInfo))
        {
            throw new RoutingException(
                "ACODE-RTE-003",
                $"Model '{overrideModelId}' not allowed in {context.OperatingMode} mode (even with user override)",
                new[] { overrideModelId }
            );
        }

        // Check availability
        if (!_modelRegistry.IsModelAvailable(overrideModelId))
        {
            throw new RoutingException(
                "ACODE-RTE-001",
                $"User override model '{overrideModelId}' is not available",
                new[] { overrideModelId }
            )
            {
                Suggestion = $"Start the model with 'ollama run {overrideModelId}'",
            };
        }

        stopwatch.Stop();

        return new RoutingDecision
        {
            ModelId = overrideModelId,
            IsFallback = false,
            SelectionReason = "user override",
            SelectedProvider = _modelRegistry.GetProviderForModel(overrideModelId),
            DecisionTimeMs = stopwatch.ElapsedMilliseconds,
            Timestamp = DateTime.UtcNow,
        };
    }

    private void LogRoutingDecision(RoutingDecision decision, AgentRole role)
    {
        _logger.LogInformation(
            "Routing decision: role={Role}, model={ModelId}, fallback={IsFallback}, "
                + "reason={Reason}, provider={Provider}, time={TimeMs}ms",
            role,
            decision.ModelId,
            decision.IsFallback,
            decision.SelectionReason,
            decision.SelectedProvider,
            decision.DecisionTimeMs
        );
    }
}
