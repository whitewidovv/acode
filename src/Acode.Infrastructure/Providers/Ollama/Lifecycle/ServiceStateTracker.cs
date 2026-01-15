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
    private readonly object _lockObject = new();
    private OllamaServiceState _currentState = OllamaServiceState.Unknown;
    private OllamaServiceState _previousState = OllamaServiceState.Unknown;
    private int _consecutiveHealthCheckFailures;
    private int _restartCount;
    private DateTime _lastStateChangeTime = DateTime.UtcNow;

    /// <summary>
    /// Event fired when service state changes.
    /// </summary>
    public event Action<OllamaServiceState, OllamaServiceState>? StateChanged;

    /// <summary>
    /// Gets the current service state.
    /// </summary>
    public OllamaServiceState CurrentState
    {
        get
        {
            lock (_lockObject)
            {
                return _currentState;
            }
        }
    }

    /// <summary>
    /// Gets the previous service state.
    /// </summary>
    public OllamaServiceState PreviousState
    {
        get
        {
            lock (_lockObject)
            {
                return _previousState;
            }
        }
    }

    /// <summary>
    /// Gets the number of consecutive health check failures.
    /// </summary>
    public int ConsecutiveHealthCheckFailures
    {
        get
        {
            lock (_lockObject)
            {
                return _consecutiveHealthCheckFailures;
            }
        }
    }

    /// <summary>
    /// Gets the number of times the service has been restarted.
    /// </summary>
    public int RestartCount
    {
        get
        {
            lock (_lockObject)
            {
                return _restartCount;
            }
        }
    }

    /// <summary>
    /// Gets the time of the last state change.
    /// </summary>
    public DateTime LastStateChangeTime
    {
        get
        {
            lock (_lockObject)
            {
                return _lastStateChangeTime;
            }
        }
    }

    /// <summary>
    /// Updates the current service state and fires state change event.
    /// </summary>
    /// <param name="newState">The new state.</param>
    public void UpdateState(OllamaServiceState newState)
    {
        Action<OllamaServiceState, OllamaServiceState>? handler;

        lock (_lockObject)
        {
            if (_currentState != newState)
            {
                var oldState = _currentState;
                _previousState = oldState;
                _currentState = newState;
                _lastStateChangeTime = DateTime.UtcNow;
                handler = StateChanged;
            }
            else
            {
                handler = null;
            }
        }

        handler?.Invoke(_previousState, _currentState);
    }

    /// <summary>
    /// Increments the consecutive health check failure counter.
    /// </summary>
    public void IncrementFailureCount()
    {
        lock (_lockObject)
        {
            _consecutiveHealthCheckFailures++;
        }
    }

    /// <summary>
    /// Resets the consecutive health check failure counter to zero.
    /// </summary>
    public void ResetFailureCount()
    {
        lock (_lockObject)
        {
            _consecutiveHealthCheckFailures = 0;
        }
    }

    /// <summary>
    /// Records a service restart.
    /// </summary>
    public void RecordRestart()
    {
        lock (_lockObject)
        {
            _restartCount++;
        }
    }

    /// <summary>
    /// Resets the restart counter to zero.
    /// </summary>
    public void ResetRestartCount()
    {
        lock (_lockObject)
        {
            _restartCount = 0;
        }
    }
}
