namespace Acode.Domain.Providers.Vllm;

/// <summary>
/// Represents the possible states of a vLLM service instance.
/// </summary>
public enum VllmServiceState
{
    /// <summary>
    /// Service is running and responding to health checks.
    /// </summary>
    Running = 0,

    /// <summary>
    /// Service startup in progress (process started but not yet responding).
    /// </summary>
    Starting = 1,

    /// <summary>
    /// Graceful shutdown in progress.
    /// </summary>
    Stopping = 2,

    /// <summary>
    /// Service stopped cleanly (shutdown complete).
    /// </summary>
    Stopped = 3,

    /// <summary>
    /// Failed to start (process exited with error or health check failed).
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Process exited unexpectedly (crash detected).
    /// </summary>
    Crashed = 5,

    /// <summary>
    /// State cannot be determined (e.g., process metadata unavailable).
    /// </summary>
    Unknown = 6
}
