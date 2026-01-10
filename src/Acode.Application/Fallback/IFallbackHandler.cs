namespace Acode.Application.Fallback;

using Acode.Application.Routing;

/// <summary>
/// Interface for handling model fallback escalation.
/// </summary>
/// <remarks>
/// <para>AC-001 to AC-007: IFallbackHandler interface definition.</para>
/// <para>Implements dependency inversion - interface in Application layer,
/// implementation in Infrastructure layer.</para>
/// </remarks>
public interface IFallbackHandler
{
    /// <summary>
    /// Gets a fallback model for the given role and context.
    /// </summary>
    /// <param name="role">The agent role requiring fallback.</param>
    /// <param name="context">The fallback context with failure details.</param>
    /// <returns>A FallbackResult indicating success or failure.</returns>
    FallbackResult GetFallback(AgentRole role, FallbackContext context);

    /// <summary>
    /// Notifies the handler of a model failure for circuit breaker tracking.
    /// </summary>
    /// <param name="modelId">The model ID that failed.</param>
    /// <param name="exception">The exception that occurred.</param>
    void NotifyFailure(string modelId, Exception exception);

    /// <summary>
    /// Notifies the handler of a model success for circuit breaker tracking.
    /// </summary>
    /// <param name="modelId">The model ID that succeeded.</param>
    void NotifySuccess(string modelId);

    /// <summary>
    /// Checks if the circuit breaker is open for a model.
    /// </summary>
    /// <param name="modelId">The model ID to check.</param>
    /// <returns>True if circuit is open (model should be skipped), false otherwise.</returns>
    bool IsCircuitOpen(string modelId);

    /// <summary>
    /// Resets the circuit breaker for a specific model.
    /// </summary>
    /// <param name="modelId">The model ID to reset.</param>
    void ResetCircuit(string modelId);

    /// <summary>
    /// Resets all circuit breakers.
    /// </summary>
    void ResetAllCircuits();

    /// <summary>
    /// Gets the current circuit state for a model.
    /// </summary>
    /// <param name="modelId">The model ID to query.</param>
    /// <returns>The circuit state information.</returns>
    CircuitStateInfo GetCircuitState(string modelId);

    /// <summary>
    /// Gets all circuit states for reporting.
    /// </summary>
    /// <returns>Dictionary of model IDs to their circuit states.</returns>
    IReadOnlyDictionary<string, CircuitStateInfo> GetAllCircuitStates();
}
