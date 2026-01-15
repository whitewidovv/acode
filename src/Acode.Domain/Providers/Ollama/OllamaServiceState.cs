namespace Acode.Domain.Providers.Ollama;

/// <summary>
/// Represents the current state of the Ollama service process.
/// </summary>
/// <remarks>
/// Enumerates all possible states during Ollama lifecycle management:
/// - Running: Process is healthy and responding to requests
/// - Starting: Process is being started and initializing
/// - Stopping: Graceful shutdown is in progress (SIGTERM sent)
/// - Stopped: Process has stopped cleanly or was never started
/// - Failed: Process failed to start or health check consistently fails
/// - Crashed: Process unexpectedly exited (was running, now gone)
/// - Unknown: Cannot determine state (shouldn't occur in normal operation)
///
/// Task 005d Functional Requirements: FR-001 to FR-005.
/// </remarks>
public enum OllamaServiceState
{
    /// <summary>
    /// Process is running and health check passes.
    /// Ready to handle inference requests.
    /// </summary>
    Running = 0,

    /// <summary>
    /// Process is being started and initializing.
    /// Not yet ready to handle requests.
    /// </summary>
    Starting = 1,

    /// <summary>
    /// Graceful shutdown is in progress (SIGTERM sent).
    /// Process is terminating normally.
    /// </summary>
    Stopping = 2,

    /// <summary>
    /// Process has stopped cleanly or was never started.
    /// Shutdown is complete.
    /// </summary>
    Stopped = 3,

    /// <summary>
    /// Process failed to start or health check failed consistently.
    /// Requires manual intervention or reconfiguration.
    /// </summary>
    Failed = 4,

    /// <summary>
    /// Process unexpectedly exited (was Running, now gone).
    /// Automatic recovery may be attempted.
    /// </summary>
    Crashed = 5,

    /// <summary>
    /// State cannot be determined.
    /// Used when state queries fail or timeout.
    /// </summary>
    Unknown = 6
}
