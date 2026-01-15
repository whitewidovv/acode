using Acode.Domain.Providers.Vllm;

namespace Acode.Infrastructure.Providers.Vllm.Lifecycle;

/// <summary>
/// Thread-safe state machine for tracking vLLM service lifecycle.
/// </summary>
public sealed class VllmServiceStateTracker
{
    private readonly object _lockObject = new();

    /// <summary>
    /// Gets the current service state.
    /// </summary>
    public VllmServiceState CurrentState { get; private set; } = VllmServiceState.Unknown;

    /// <summary>
    /// Gets the process ID (if running).
    /// </summary>
    public int? ProcessId { get; private set; }

    /// <summary>
    /// Gets when the service started running (UTC).
    /// </summary>
    public DateTime? UpSinceUtc { get; private set; }

    /// <summary>
    /// Gets when the last health check occurred (UTC).
    /// </summary>
    public DateTime? LastHealthCheckUtc { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the last health check succeeded.
    /// </summary>
    public bool LastHealthCheckHealthy { get; private set; }

    /// <summary>
    /// Gets the error message (if any).
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Transitions to a new service state.
    /// </summary>
    /// <param name="newState">The new state to transition to.</param>
    public void Transition(VllmServiceState newState)
    {
        lock (_lockObject)
        {
            // When transitioning to Running, set UpSinceUtc
            if (newState == VllmServiceState.Running && CurrentState != VllmServiceState.Running)
            {
                UpSinceUtc = DateTime.UtcNow;
            }

            CurrentState = newState;
        }
    }

    /// <summary>
    /// Marks the service as healthy (successful health check).
    /// </summary>
    public void MarkHealthy()
    {
        lock (_lockObject)
        {
            LastHealthCheckUtc = DateTime.UtcNow;
            LastHealthCheckHealthy = true;
        }
    }

    /// <summary>
    /// Marks the service as unhealthy (failed health check).
    /// </summary>
    public void MarkUnhealthy()
    {
        lock (_lockObject)
        {
            LastHealthCheckUtc = DateTime.UtcNow;
            LastHealthCheckHealthy = false;
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
            ProcessId = processId;
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
            ErrorMessage = errorMessage;
        }
    }
}
