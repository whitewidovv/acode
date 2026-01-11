namespace Acode.Infrastructure.Fallback;

using System.Collections.Concurrent;
using Acode.Application.Fallback;
using Acode.Application.Routing;
using Acode.Infrastructure.Routing;
using Microsoft.Extensions.Logging;

/// <summary>
/// Handles model fallback escalation with circuit breaker pattern.
/// </summary>
/// <remarks>
/// <para>AC-008 to AC-013: FallbackHandler implementation.</para>
/// <para>AC-031 to AC-038: Circuit breaker integration.</para>
/// <para>AC-055 to AC-061: Logging and observability.</para>
/// </remarks>
public sealed class FallbackHandlerWithCircuitBreaker : IFallbackHandler
{
    private readonly ModelRegistry _registry;
    private readonly IFallbackConfiguration _config;
    private readonly ILogger<FallbackHandlerWithCircuitBreaker> _logger;
    private readonly ConcurrentDictionary<string, CircuitBreaker> _circuits = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FallbackHandlerWithCircuitBreaker"/> class.
    /// </summary>
    /// <param name="registry">The model registry for availability checks.</param>
    /// <param name="config">The fallback configuration.</param>
    /// <param name="logger">The logger.</param>
    public FallbackHandlerWithCircuitBreaker(
        ModelRegistry registry,
        IFallbackConfiguration config,
        ILogger<FallbackHandlerWithCircuitBreaker> logger
    )
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public FallbackResult GetFallback(AgentRole role, FallbackContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var chain = GetFallbackChain(role);
        var triedModels = new List<string>();
        var failureReasons = new Dictionary<string, string>();

        if (chain.Count == 0)
        {
            _logger.LogError("No fallback chain configured for role {Role}", role);

            return FallbackResult.Failed(
                $"No fallback chain configured for role {role}",
                triedModels
            );
        }

        foreach (var modelId in chain)
        {
            // Skip the original failing model
            if (modelId == context.OriginalModel)
            {
                continue;
            }

            triedModels.Add(modelId);

            // Check circuit breaker
            var circuit = GetOrCreateCircuit(modelId);
            if (!circuit.ShouldAllow())
            {
                var reason = $"circuit breaker {circuit.State}";
                failureReasons[modelId] = reason;

                _logger.LogDebug("Skipping {ModelId}: {Reason}", modelId, reason);
                continue;
            }

            // Check availability
            if (!_registry.IsModelAvailable(modelId))
            {
                var reason = "unavailable";
                failureReasons[modelId] = reason;

                _logger.LogDebug("Skipping {ModelId}: {Reason}", modelId, reason);
                continue;
            }

            // Found available fallback
            _logger.LogWarning(
                "Fallback triggered: original={OriginalModel}, fallback={FallbackModel}, "
                    + "role={Role}, trigger={Trigger}, session={SessionId}",
                context.OriginalModel,
                modelId,
                role,
                context.Trigger,
                context.SessionId
            );

            return FallbackResult.Succeeded(
                modelId,
                $"{context.OriginalModel} {context.Trigger.ToString().ToLowerInvariant()}, using {modelId}",
                triedModels
            );
        }

        // All fallbacks exhausted
        _logger.LogError(
            "All fallbacks exhausted for role {Role}. Tried: {Models}. "
                + "Reasons: {Reasons}, session={SessionId}",
            role,
            string.Join(", ", triedModels),
            string.Join(", ", failureReasons.Select(kvp => $"{kvp.Key}={kvp.Value}")),
            context.SessionId
        );

        return FallbackResult.Failed(
            $"All fallbacks exhausted for role {role}. Tried: {string.Join(", ", triedModels)}",
            triedModels,
            failureReasons
        );
    }

    /// <inheritdoc/>
    public void NotifyFailure(string modelId, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (string.IsNullOrWhiteSpace(modelId))
        {
            return;
        }

        var circuit = GetOrCreateCircuit(modelId);
        circuit.RecordFailure();

        if (circuit.State == CircuitState.Open)
        {
            _logger.LogWarning(
                "Circuit opened for {ModelId} after {Failures} failures. "
                    + "Cooling for {CoolingSeconds}s. Error: {Error}",
                modelId,
                circuit.FailureCount,
                _config.CoolingPeriod.TotalSeconds,
                exception.Message
            );
        }
        else
        {
            _logger.LogDebug(
                "Failure recorded for {ModelId}: count={Count}, threshold={Threshold}",
                modelId,
                circuit.FailureCount,
                _config.FailureThreshold
            );
        }
    }

    /// <inheritdoc/>
    public void NotifySuccess(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return;
        }

        if (_circuits.TryGetValue(modelId, out var circuit))
        {
            var previousState = circuit.State;
            circuit.RecordSuccess();

            if (previousState != CircuitState.Closed)
            {
                _logger.LogInformation(
                    "Circuit closed for {ModelId} after successful request (was {PreviousState})",
                    modelId,
                    previousState
                );
            }
        }
    }

    /// <inheritdoc/>
    public bool IsCircuitOpen(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return false;
        }

        var circuit = GetOrCreateCircuit(modelId);
        return circuit.State == CircuitState.Open;
    }

    /// <inheritdoc/>
    public void ResetCircuit(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return;
        }

        if (_circuits.TryGetValue(modelId, out var circuit))
        {
            circuit.Reset();
            _logger.LogInformation("Circuit reset for {ModelId}", modelId);
        }
    }

    /// <inheritdoc/>
    public void ResetAllCircuits()
    {
        foreach (var kvp in _circuits)
        {
            kvp.Value.Reset();
        }

        _logger.LogInformation("All circuits reset ({Count} circuits)", _circuits.Count);
    }

    /// <inheritdoc/>
    public CircuitStateInfo GetCircuitState(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return new CircuitStateInfo
            {
                ModelId = modelId ?? string.Empty,
                State = CircuitState.Closed,
                FailureCount = 0,
            };
        }

        var circuit = GetOrCreateCircuit(modelId);
        return circuit.GetStateInfo(modelId);
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, CircuitStateInfo> GetAllCircuitStates()
    {
        var result = new Dictionary<string, CircuitStateInfo>();

        foreach (var kvp in _circuits)
        {
            result[kvp.Key] = kvp.Value.GetStateInfo(kvp.Key);
        }

        return result;
    }

    private IReadOnlyList<string> GetFallbackChain(AgentRole role)
    {
        // Role-specific chain takes precedence (AC-012)
        var roleChain = _config.GetRoleChain(role);
        if (roleChain.Count > 0)
        {
            return roleChain;
        }

        // Fall back to global chain
        return _config.GetGlobalChain();
    }

    private CircuitBreaker GetOrCreateCircuit(string modelId)
    {
        return _circuits.GetOrAdd(
            modelId,
            _ => new CircuitBreaker(_config.FailureThreshold, _config.CoolingPeriod)
        );
    }
}
