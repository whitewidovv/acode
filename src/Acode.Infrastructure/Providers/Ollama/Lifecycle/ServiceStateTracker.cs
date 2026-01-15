using Acode.Domain.Providers.Ollama;

namespace Acode.Infrastructure.Providers.Ollama.Lifecycle;

/// <summary>
/// Internal state machine for tracking Ollama service lifecycle state.
/// </summary>
/// <remarks>
/// Manages state transitions, failure counters, and restart tracking.
/// Task 005d Functional Requirements: FR-014 to FR-050.
/// </remarks>
internal sealed class ServiceStateTracker
{
    private OllamaServiceState _currentState = OllamaServiceState.Unknown;
    private OllamaServiceState _previousState = OllamaServiceState.Unknown;
    private int _consecutiveHealthCheckFailures;
    private int _restartCount;

    /// <summary>
    /// Event fired when service state changes.
    /// </summary>
    public event Action<OllamaServiceState, OllamaServiceState>? StateChanged;

    /// <summary>
    /// Gets the current service state.
    /// </summary>
    public OllamaServiceState CurrentState => _currentState;

    /// <summary>
    /// Gets the previous service state.
    /// </summary>
    public OllamaServiceState PreviousState => _previousState;

    /// <summary>
    /// Gets the number of consecutive health check failures.
    /// </summary>
    public int ConsecutiveHealthCheckFailures => _consecutiveHealthCheckFailures;

    /// <summary>
    /// Gets the number of times the service has been restarted.
    /// </summary>
    public int RestartCount => _restartCount;

    /// <summary>
    /// Gets the time of the last state change.
    /// </summary>
    public DateTime LastStateChangeTime { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Updates the current service state and fires state change event.
    /// </summary>
    /// <param name="newState">The new state.</param>
    public void UpdateState(OllamaServiceState newState)
    {
        if (_currentState != newState)
        {
            var oldState = _currentState;
            _previousState = oldState;
            _currentState = newState;
            LastStateChangeTime = DateTime.UtcNow;
            StateChanged?.Invoke(oldState, newState);
        }
    }

    /// <summary>
    /// Increments the consecutive health check failure counter.
    /// </summary>
    public void IncrementFailureCount()
    {
        _consecutiveHealthCheckFailures++;
    }

    /// <summary>
    /// Resets the consecutive health check failure counter to zero.
    /// </summary>
    public void ResetFailureCount()
    {
        _consecutiveHealthCheckFailures = 0;
    }

    /// <summary>
    /// Records a service restart.
    /// </summary>
    public void RecordRestart()
    {
        _restartCount++;
    }

    /// <summary>
    /// Resets the restart counter to zero.
    /// </summary>
    public void ResetRestartCount()
    {
        _restartCount = 0;
    }
}
