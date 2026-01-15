using Acode.Domain.Providers.Vllm;

namespace Acode.Infrastructure.Providers.Vllm.Lifecycle;

/// <summary>
/// Thread-safe state machine for tracking vLLM service lifecycle.
/// </summary>
public sealed class VllmServiceStateTracker
{
    private readonly object _lockObject = new();

    private VllmServiceState _currentState = VllmServiceState.Unknown;
    private int? _processId;
    private DateTime? _upSinceUtc;
    private DateTime? _lastHealthCheckUtc;
    private bool _lastHealthCheckHealthy;
    private string? _errorMessage;

    /// <summary>
    /// Gets the current service state.
    /// </summary>
    public VllmServiceState CurrentState
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
    /// Gets the process ID (if running).
    /// </summary>
    public int? ProcessId
    {
        get
        {
            lock (_lockObject)
            {
                return _processId;
            }
        }
    }

    /// <summary>
    /// Gets when the service started running (UTC).
    /// </summary>
    public DateTime? UpSinceUtc
    {
        get
        {
            lock (_lockObject)
            {
                return _upSinceUtc;
            }
        }
    }

    /// <summary>
    /// Gets when the last health check occurred (UTC).
    /// </summary>
    public DateTime? LastHealthCheckUtc
    {
        get
        {
            lock (_lockObject)
            {
                return _lastHealthCheckUtc;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the last health check succeeded.
    /// </summary>
    public bool LastHealthCheckHealthy
    {
        get
        {
            lock (_lockObject)
            {
                return _lastHealthCheckHealthy;
            }
        }
    }

    /// <summary>
    /// Gets the error message (if any).
    /// </summary>
    public string? ErrorMessage
    {
        get
        {
            lock (_lockObject)
            {
                return _errorMessage;
            }
        }
    }

    /// <summary>
    /// Transitions to a new service state.
    /// </summary>
    /// <param name="newState">The new state to transition to.</param>
    public void Transition(VllmServiceState newState)
    {
        lock (_lockObject)
        {
            // When transitioning to Running, set UpSinceUtc
            if (newState == VllmServiceState.Running && _currentState != VllmServiceState.Running)
            {
                _upSinceUtc = DateTime.UtcNow;
            }

            _currentState = newState;
        }
    }

    /// <summary>
    /// Marks the service as healthy (successful health check).
    /// </summary>
    public void MarkHealthy()
    {
        lock (_lockObject)
        {
            _lastHealthCheckUtc = DateTime.UtcNow;
            _lastHealthCheckHealthy = true;
        }
    }

    /// <summary>
    /// Marks the service as unhealthy (failed health check).
    /// </summary>
    public void MarkUnhealthy()
    {
        lock (_lockObject)
        {
            _lastHealthCheckUtc = DateTime.UtcNow;
            _lastHealthCheckHealthy = false;
        }
    }

    /// <summary>
    /// Sets the process ID of the vLLM service.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    public void SetProcessId(int processId)
    {
        lock (_lockObject)
        {
            _processId = processId;
        }
    }

    /// <summary>
    /// Sets the error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    public void SetErrorMessage(string? errorMessage)
    {
        lock (_lockObject)
        {
            _errorMessage = errorMessage;
        }
    }
}
